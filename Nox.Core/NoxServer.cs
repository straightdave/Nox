using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Nox.Core.Extension;

namespace Nox.Core
{
	public class NoxServer
	{
		private Thread _loopingListenerThread;
		private TcpListener _listener;
		private ExtensionManager _extManager;
		
		public int Port { get; private set; }

		public int MaxConnections { get; private set; }

		public NoxServer(int port = 12345, int maxConnections = 100)
		{
			Port = port;
			MaxConnections = maxConnections;
			_listener = new TcpListener(IPAddress.Loopback, port);
			_extManager = new ExtensionManager();
			ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
		}

		#region Shortcut for extensions
		public NoxServer RegisterExt(INoxExtension ext)
		{
			_extManager.RegExt(ext);
			return this;
		}

		public NoxServer RegisterExt<T>() where T : INoxExtension
		{
			_extManager.RegExt<T>();
			return this;
		}

		public NoxServer RegisterExt(Action<TcpClient> process)
		{
			_extManager.RegExt(process);
			return this;
		}
		#endregion

		public NoxServer Start(ThreadPriority priority = ThreadPriority.Normal)
		{
			_loopingListenerThread = new Thread(() =>
			{
				_listener.Start();

				var sem = new Semaphore(MaxConnections, MaxConnections);

				while (true)
				{
					sem.WaitOne();

					_listener.AcceptTcpClientAsync().ContinueWith(async task =>
					{
						TcpClient tcpClient = await task;
						await Task.Run(() => _extManager.ProcessAllExt(tcpClient));
						if (tcpClient.Connected)
						{
							tcpClient.Close();
						}
						sem.Release();
					});
				}
			});

			_loopingListenerThread.Priority = priority;
			_loopingListenerThread.Start();
			return this;
		}

		public void Stop()
		{
			_listener.Stop();
			_loopingListenerThread.Abort();
			if (_loopingListenerThread.IsAlive)
			{
				_loopingListenerThread.Join();
			}
		}
	}
}
