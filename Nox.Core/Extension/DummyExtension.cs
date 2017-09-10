using System;
using System.Net.Sockets;

namespace Nox.Core.Extension
{
	class DummyExtension : INoxExtension
	{
		private Action<TcpClient> _action;

		public DummyExtension(Action<TcpClient> process)
		{
			_action = process;
		}

		public event NoxExtensionEventHandler BeginProcessing;
		public event NoxExtensionEventHandler EndProcessing;
		public event NoxExtensionEventHandler ErrorOccured;

		public void Process(TcpClient tcpClient)
		{
			_action.Invoke(tcpClient);
		}
	}
}
