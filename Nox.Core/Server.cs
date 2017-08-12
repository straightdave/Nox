using Nox.Core.Extension;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Core
{
	public class Server
	{
		private const string LOCALHOST = "127.0.0.1";

		internal ExtensionManager ExtManager { get; private set; }

		internal Dictionary<int, bool> Listenees { get; private set; }

		public Server()
		{
			Listenees = new Dictionary<int, bool>();
			ExtManager = new ExtensionManager();
		}

		public Server ListenTo(int port, bool isHttps = false)
		{
			Listenees[port] = isHttps;
			return this;
		}

		public Server RegisterExt<T>() where T : INoxExtension
		{
			ExtManager.RegExt<T>();
			return this;
		}

		public void Start()
		{
			var listener = new HttpListener();
			foreach (var kv in Listenees)
			{
				var _proto = kv.Value ? "https://" : "http://";
				listener.Prefixes.Add($"{_proto}{LOCALHOST}:{kv.Key}/");
			};

			listener.Start();

			while (true)
			{
				var context = listener.GetContext();
				ExtManager.ProcessAllExt(context);
			}
		}
	}
}
