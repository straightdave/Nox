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

		// required events
		public event NoxExtensionEventHandler BeginProcessing;
		public event NoxExtensionEventHandler EndProcessing;
		public event NoxExtensionEventHandler ErrorOccured;

		// additional events
		public event NoxExtensionEventHandler PrintInfoEvent;

		public void Process(TcpClient tcpClient)
		{
			BeginProcessing?.Invoke(this, null);

			try
			{
				var ns = tcpClient.GetStream();

				// read into a memory stream (which can rewind)
				MemoryStream ms0 = new MemoryStream();
				byte[] buff0 = new byte[2048];
				while (ns.DataAvailable)
				{
					var reads = ns.Read(buff0, 0, buff0.Length);
					ms0.Write(buff0, 0, reads);
				}
				ms0.Seek(0, SeekOrigin.Begin);

				// get metadata lines
				List<string> lines = ReadMetaLinesFromMemoryStream(ms0);

				// read http cmd line
				var cmd = lines[0];
				PrintInfo(cmd);

				var cmd_splits = cmd.Split(SPACE_SPLIT, 3);
				var verb = GetHttpVerbFromString(cmd_splits[0]);
				var url = cmd_splits[1];
				var proto = cmd_splits[2];

				// respond with 'GET /_nox_'
				if (verb == Verb.GET && url.Contains("_nox_"))
				{
					PrintInfo("_nox_ is called");
					var _writer = new StreamWriter(ns);
					_writer.WriteLine($"{proto} 200 OK");
					_writer.WriteLine($"Proxy-agent: dave-nox");
					_writer.WriteLine();
					_writer.Flush();
					_writer.Close();
					return;
				}

				// read headers
				var headers = new Dictionary<string, string>();
				lines.GetRange(1, lines.Count - 1).ForEach(x =>
				{
					var _splits = x.Split(COLON_SPLIT, 2);
					headers.Add(_splits[0].Trim().ToLowerInvariant(), _splits[1].Trim());
				});

				// remove connection persistence in headers
				if (headers.ContainsKey("connection"))
				{
					var _tokens = headers["connection"].Split(COMMA_SPLIT);
					foreach (string t in _tokens)
					{
						headers.Remove(t.Trim());
					}
					headers["connection"] = "close";
				}
				else
				{
					headers.Add("connection", "close");
				}

				// RFC2612: Http 1.1 requests must have 'Host' header
				// TODO: respond with error message if no 'Host' exists
				var r_host = headers["host"];
				var r_port = 80;
				if (r_host.Contains(":"))
				{
					int.TryParse(r_host.Split(COLON_SPLIT, 2)[1], out r_port);
					r_host = r_host.Split(COLON_SPLIT, 2)[0];
				}

				PrintInfo($"Original Host: {r_host}");

				// create connection to origin server
				var q = Dns.GetHostEntry(r_host);
				TcpClient c = new TcpClient();
				c.Connect(q.AddressList, r_port);
				PrintInfo("connected to original server");
				var cs = c.GetStream();

				// copy headers
				var writer = new StreamWriter(cs);
				writer.WriteLine(cmd);
				foreach (var item in headers)
				{
					PrintInfo($"{item.Key} => {item.Value}");
					writer.WriteLine($"{item.Key}: {item.Value}");
				}
				writer.WriteLine();  // must have

				// copy request body if any
				if (verb == Verb.POST || verb == Verb.PUT)
				{
					ms0.CopyTo(cs);
				}
				writer.Flush();
				PrintInfo("Request sent to original server");

				// read http response from stream, into a new memory stream
				MemoryStream ms1 = new MemoryStream();
				byte[] buff = new byte[2048];
				while (cs.DataAvailable)
				{
					var reads = cs.Read(buff, 0, buff.Length);
					ms1.Write(buff, 0, reads);
				}
				ms1.Seek(0, SeekOrigin.Begin);
				writer.Close(); // close inner stream (cs) & its writer
				c.Close();    // close inner connection
				PrintInfo($"Get response. Len:{ms1.Length}. Original server disconnected.");

				var resp_meta_lines = ReadMetaLinesFromMemoryStream(ms1);
				PrintInfo($"meta lines {resp_meta_lines.Count}");

				ms1.Seek(0, SeekOrigin.Begin);

				PrintInfo($"response first line: {resp_meta_lines[0]}");

				// write response to client
				{
					ms1.CopyTo(ns);
					ns.Flush();
				}
			}
			catch(Exception ex)
			{
				var ea = new NoxEventArgs { Message = "Ex happened", Error = ex };
				PrintInfoEvent?.Invoke(this, ea);
				ErrorOccured?.Invoke(this, ea);
			}
			finally
			{
				EndProcessing?.Invoke(this, null);
			}
		}

		static Verb GetHttpVerbFromString(string verb)
		{
			var v = Verb.UNKNOWN;
			Enum.TryParse(verb.ToUpperInvariant(), out v);
			return v;
		}

		static string GetValueOrNull(Dictionary<string, string> dict, string key)
		{
			return dict.ContainsKey(key) ? dict[key] : null;
		}

		static List<string> ReadMetaLinesFromMemoryStream(MemoryStream ms)
		{
			var result = new List<string>();
			int line_len = 0;
			int new_lines = 0;
			while (true)
			{
				var cc = ms.ReadByte();
				if (cc < 0) break;

				if (cc == 10 || cc == 13)
				{
					new_lines++;

					if (new_lines == 4)
					{
						break; // '\r\n\r\n': end of headers or message
					}

					if (line_len > 0)
					{
						ms.Seek(ms.Position - line_len - 1, SeekOrigin.Begin);
						byte[] _line_bytes = new byte[line_len];
						ms.Read(_line_bytes, 0, line_len);

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
