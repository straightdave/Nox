using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Nox.Core.Extension;

namespace Nox.Extension
{
	public class NoxProxy : INoxExtension
	{
		private static char[] SPACE_SPLIT = new char[] { ' ' };
		private static char[] COLON_SPLIT = new char[] { ':' };
		private static char[] COMMA_SPLIT = new char[] { ',' };
		private static string PAYLOAD_DELIMITER = "---nox---";
		
		public event NoxExtensionEventHandler ErrorOccured;
		public event NoxExtensionEventHandler PrintInfoEvent;

		public void Process(TcpClient tcpClient)
		{
			try
			{
				NetworkStream ns = tcpClient.GetStream();

				// copy into a memory stream (which can rewind)
				// Note: ns maybe not all consumed if it has payload
				MemoryStream reqInMem = CopyStreamFrom(ns);

				// get raw request content into lines
				List<string> reqLines = ReadIntoLines(reqInMem);

				// read http cmd line
				string httpCmd = reqLines[0];
				var cmd_splits = httpCmd.Split(SPACE_SPLIT, 3);
				Verb verb = GetHttpVerbFromString(cmd_splits[0]);
				string url = cmd_splits[1];
				string proto = cmd_splits[2];

				// read headers
				var headers = new Dictionary<string, string>();
				reqLines.GetRange(1, reqLines.IndexOf(PAYLOAD_DELIMITER) - 1).ForEach(x =>
				{
					var _splits = x.Split(COLON_SPLIT, 2);
					headers.Add(_splits[0].Trim().ToLowerInvariant(), _splits[1].Trim());
				});

				// respond with 'GET /_nox_'
				if (verb == Verb.GET && url.Contains("_nox_"))
				{
					var _w = new StreamWriter(ns);
					_w.WriteLine($"{proto} 200 OK");
					_w.WriteLine("Proxy-agent: dave-nox");
					_w.WriteLine("Content-Type: text/html");
					_w.WriteLine("Connection: close");
					_w.WriteLine();
					_w.WriteLine("<html><body><h1>nox is running</h1></body></html>");
					_w.Flush();
					_w.Close();
					reqInMem.Close();
					return;
				}
				
				// read payload (if any)
				if (verb == Verb.POST || verb == Verb.PUT)
				{
					PrintInfo("Reading payload");
					if (headers.ContainsKey("content-length"))
					{
						int len = 0;
						int.TryParse(headers["content-length"], out len);

						if (len > 0)
						{
							// ms0 is at the right position
							byte[] buffer = new byte[len];
							int _reads = ns.Read(buffer, 0, buffer.Length);
							reqInMem.Write(buffer, 0, _reads);
						}

						PrintInfo("Raw request after reading payload");
						PrintInfo(DumpContentOf(reqInMem));
					}
				}

				// RFC2616: as a proxy, to remove connection persistence in headers
				PrintInfo("Modify connection headers");
				if (headers.ContainsKey("connection"))
				{
					var _tokens = headers["connection"].Split(COMMA_SPLIT);
					foreach (string t in _tokens)
					{
						headers.Remove(t.Trim().ToLowerInvariant());
					}
					headers["connection"] = "close";
				}
				else
				{
					headers.Add("connection", "close");
				}

				// RFC2616: Http 1.1 requests must have 'Host' header
				// TODO: respond with error message if no 'Host' exists
				var r_host = headers["host"];
				var r_port = 80;
				if (r_host.Contains(":"))
				{
					int.TryParse(r_host.Split(COLON_SPLIT, 2)[1], out r_port);
					r_host = r_host.Split(COLON_SPLIT, 2)[0];
				}
				
				// create connection to origin server
				var q = Dns.GetHostEntry(r_host);
				TcpClient c = new TcpClient();
				c.Connect(q.AddressList, r_port);
				PrintInfo($"Connected to {r_host}");

				NetworkStream cs = c.GetStream();

				// copy http cmd and headers
				var writer = new StreamWriter(cs);
				writer.WriteLine(httpCmd);
				foreach (var item in headers)
				{
					writer.WriteLine($"{item.Key}: {item.Value}");
				}
				writer.WriteLine();  // must have
				writer.Flush();
				
				// copy request body if any
				if (verb == Verb.POST || verb == Verb.PUT)
				{
					// TODO!!! to now pos is 0;
					var pos = reqInMem.Position;
					var reader = new StreamReader(reqInMem);
					var body = reader.ReadToEnd();
					reqInMem.Seek(pos, SeekOrigin.Begin);
					reqInMem.CopyTo(cs);
					cs.Flush();
				}

				// wait for response
				int timeout = 0;
				while (!cs.DataAvailable)
				{
					System.Threading.Thread.Sleep(50);

					if (timeout++ > 100)
					{
						var _w = new StreamWriter(ns);
						_w.WriteLine($"{proto} 505 Timeout");
						_w.WriteLine("Proxy-agent: dave-nox");
						_w.WriteLine("Content-Type: text/html");
						_w.WriteLine("Connection: close");
						_w.WriteLine();
						_w.WriteLine("<html><body><h1>proxing timeout</h1></body></html>");
						_w.Flush();

						_w.Close();
						cs.Close();
						reqInMem.Close();
						return;
					}
				}

				MemoryStream ms1 = CopyStreamFrom(cs, 4096);
				PrintInfo("-- dump response --");
				PrintInfo(DumpContentOf(ms1));
				PrintInfo("-- end dumping response --");




				var _writer = new StreamWriter(ns);
				_writer.WriteLine($"{proto} 200 OK");
				_writer.WriteLine("Proxy-agent: dave-nox");
				_writer.WriteLine("Content-Type: text/html");
				_writer.WriteLine("Connection: close");
				_writer.WriteLine();
				_writer.WriteLine("<html><body><h1>nox is running</h1></body></html>");
				_writer.Flush();
				_writer.Close();
				reqInMem.Close();
				return;
			}
			catch (Exception ex)
			{
				var ea = new NoxEventArgs { Error = ex };
				ErrorOccured?.Invoke(this, ea);
			}
		}

		static MemoryStream CopyStreamFrom(NetworkStream netStream, int bufferSize = 2048)
		{
			if (netStream.CanRead)
			{
				MemoryStream ms = new MemoryStream();
				byte[] buffer = new byte[bufferSize];
				int _read = 0;
				do
				{
					_read = netStream.Read(buffer, 0, buffer.Length);
					ms.Write(buffer, 0, _read);
				}
				while (netStream.DataAvailable);
				ms.Position = 0;
				return ms;
			}
			else
			{
				throw new InvalidOperationException("Underlying network stream not readable");
			}
		}

		static string DumpContentOf(MemoryStream memStream)
		{
			memStream.Position = 0;
			var reader = new StreamReader(memStream);
			var content = reader.ReadToEnd();
			memStream.Position = 0;
			return content;
		}
		
		static Verb GetHttpVerbFromString(string verb)
		{
			var v = Verb.UNKNOWN;
			Enum.TryParse(verb.ToUpperInvariant(), out v);
			return v;
		}

		static List<string> ReadIntoLines(Stream stream)
		{
			var result = new List<string>();
			int line_len = 0;
			int new_lines = 0;
			while (true)
			{
				var cc = stream.ReadByte();
				if (cc < 0) break;

				if (cc == 10 || cc == 13)  // new line characters
				{
					new_lines++;

					if (new_lines == 4)
					{
						result.Add(PAYLOAD_DELIMITER);
						new_lines = 0;
						continue;
					}

					if (line_len > 0)
					{
						stream.Seek(stream.Position - line_len - 1, SeekOrigin.Begin);
						byte[] _line_bytes = new byte[line_len];
						stream.Read(_line_bytes, 0, line_len);

						result.Add(Encoding.UTF8.GetString(_line_bytes));
						line_len = 0;
					}
					else
					{
						continue;
					}
				}
				else
				{
					new_lines = 0;
					line_len += 1;
				}
			}

			return result;
		}

		void PrintInfo(string message)
		{
			PrintInfoEvent?.Invoke(this, new NoxEventArgs { Message = message });
		}
	}

	enum Verb
	{
		UNKNOWN = 0,
		GET = 1,
		POST = 2,
		PUT = 3,
		DELETE = 4,
		OPTION = 5,
		CONNECT = 6,
		HEAD = 7
	}
}
