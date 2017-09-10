using System;
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
				np.BeginProcessing += (sender, args) =>
				{
					Console.WriteLine($"Get connection");
				};

				np.PrintInfoEvent += (sender, args) =>
				{
					Console.WriteLine(args.Message);
				};

				np.ErrorOccured += (sender, args) =>
				{
					Console.WriteLine($"ex: {args.Error.Message}");
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
			var req = (HttpWebRequest)WebRequest.Create(url);
			var resp = req.GetResponse() as HttpWebResponse;
			Assert.AreEqual(200, (int)resp.StatusCode);
			Assert.AreEqual("dave-nox", resp.Headers["Proxy-agent"]);
		}

		[TestMethod]
		public void BypassHttpAsProxy()
		{
			var url = "http://www.ietf.org/rfc/rfc7230.txt";
			var req = (HttpWebRequest)WebRequest.Create(url);
			req.UserAgent = "visual studio";
			//req.Proxy = new WebProxy("127.0.0.1", port);

			var resp = req.GetResponse() as HttpWebResponse;
			StreamReader reader = new StreamReader(resp.GetResponseStream());
			Console.WriteLine(reader.ReadToEnd());
		}

		[TestCleanup]
		public void TestCleanUp()
		{
			nox.Stop();
		}
	}
}
