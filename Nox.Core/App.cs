using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nox.Core.Extension;

namespace Nox.Core
{
	public class App
	{
		private Server server;

		public App()
		{
			server = new Server();
		}

		public App RegExt<T>() where T : INoxExtension
		{
			server.RegisterExt<T>();
			return this;
		}

		public void StartServer(int port = 2333)
		{
			// create new AppDomain
			// with Nox.Server
			// then start it
			server.ListenTo(port);
			server.Start();
		}

		public void StopServer()
		{
			// stop server (if possible)
			// then delete its AppDomain
		}
	}
}
