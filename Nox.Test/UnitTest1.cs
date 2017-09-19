using System;
using System.Diagnostics;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nox.Core;
using Nox.Extension;
using System.IO;

namespace Nox.Test
{
	[TestClass]
	public class UnitTest1
	{
		private const int port = 13579;
		private NoxServer nox;

		[TestInitialize]
		public void TestInit()
		{
			try
			{
				var np = new NoxProxy();
				np.PrintInfoEvent += (sender, args) =>
				{
					Console.WriteLine(args.Message);
				};

				np.ErrorOccured += (sender, args) =>
				{
					Console.WriteLine(args.Error.Message);
				};

				nox = new NoxServer(port: port, maxConnections: 10)
					.RegisterExt(np)
					.Start();
			}
			catch
			{
				Assert.Fail("Cannot initialize or start Nox server");
			}
		}

		[TestMethod]
		public void NoxIsWorking()
		{
			var url = $"http://localhost:{port}/_nox_";
			var req = WebRequest.Create(url) as HttpWebRequest;
			var resp = req.GetResponse() as HttpWebResponse;
			Assert.AreEqual(200, (int)resp.StatusCode);
			Assert.AreEqual("dave-nox", resp.Headers["Proxy-agent"]);
		}

		[TestMethod]
		public void BypassHttpAsProxy()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			var url = "http://www.ietf.org/rfc/rfc7230.txt";
			var req = WebRequest.Create(url) as HttpWebRequest;
			req.UserAgent = "visual studio";
			req.Proxy = new WebProxy("127.0.0.1", port);

			var resp = req.GetResponse() as HttpWebResponse;
			Console.WriteLine($"[{sw.ElapsedMilliseconds}] received response");

			Assert.IsNotNull(resp);
			Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
			Assert.AreEqual("dave-nox", resp.Headers["Proxy-agent"]);
		}

		[TestMethod]
		public void BypassPOSTRequest()
		{
			var url = "http://posttestserver.com/post.php";
			var req = WebRequest.Create(url) as HttpWebRequest;
			req.Proxy = new WebProxy("127.0.0.1", port);
			req.Method = "POST";
			req.UserAgent = "visual studio";
			var writer = new StreamWriter(req.GetRequestStream());
			writer.WriteLine($"thank this site ------- {DateTime.Now.ToString()}");
			writer.Flush();
			
			var resp = req.GetResponse() as HttpWebResponse;
			Assert.IsNotNull(resp);
			Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
		}

		[TestMethod]
		public void TimeWithoutProxy()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			var url = "http://www.ietf.org/rfc/rfc7230.txt";
			var req = WebRequest.Create(url) as HttpWebRequest;
			req.UserAgent = "visual studio";
			req.Proxy = null;

			var resp = req.GetResponse() as HttpWebResponse;
			Console.WriteLine($"[{sw.ElapsedMilliseconds}] received response");
		}

		[TestCleanup]
		public void TestCleanUp()
		{
			nox.Stop();
		}
	}
}
