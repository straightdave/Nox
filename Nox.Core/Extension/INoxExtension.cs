using System;
using System.Net.Sockets;

namespace Nox.Core.Extension
{
	public class NoxEventArgs : EventArgs
	{
		public string Message { get; set; }

		public Exception Error { get; set; }
	}

	public delegate void NoxExtensionEventHandler(object sender, NoxEventArgs args);

	public interface INoxExtension
	{
		// required events
		event NoxExtensionEventHandler BeginProcessing;
		event NoxExtensionEventHandler EndProcessing;
		event NoxExtensionEventHandler ErrorOccured;

		void Process(TcpClient tcpClient);
	}
}
