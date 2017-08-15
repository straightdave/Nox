using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Nox.Core.Extension;

namespace Nox.Core
{
	public class NoxServer
	{
		private const string LOCALHOST = "127.0.0.1";

		internal ExtensionManager ExtManager { get; private set; }

		internal Dictionary<int, bool> Listenees { get; private set; }

		public NoxServer()
		{
			Listenees = new Dictionary<int, bool>();
			ExtManager = new ExtensionManager();
		}

		public NoxServer ListenTo(int port, bool isHttps = false)
		{
			Listenees[port] = isHttps;
			return this;
		}

		public NoxServer RegisterExt<T>() where T : INoxExtension
		{
			ExtManager.RegExt<T>();
			return this;
		}

		public NoxServer RegisterExt(Action<HttpListenerContext> process)
		{
			ExtManager.RegExt(process);
			return this;
		}

		/// <summary>
		/// TODO: start the listener in a thread (or process?)
		/// </summary>
		public void Start()
		{
			using (var listener = new HttpListener())
			{
				foreach (var kv in Listenees)
				{
					var _proto = kv.Value ? "https://" : "http://";
					listener.Prefixes.Add($"{_proto}{LOCALHOST}:{kv.Key}/");
				}

				listener.Start();

				var sem = new Semaphore(100, 100);

				while (true)
				{
					sem.WaitOne();

					listener.GetContextAsync().ContinueWith(async act =>
					{
						sem.Release();
						var _context = await act;
						await Task.Run(() => ExtManager.ProcessAllExt(_context));
					});
				}
			}
		}
	}
}
