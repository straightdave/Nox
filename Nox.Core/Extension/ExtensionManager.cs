using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Nox.Core.Extension
{
	internal class ExtensionManager
	{
		public List<INoxExtension> ExtList { get; private set; }

		public ExtensionManager()
		{
			ExtList = new List<INoxExtension>();
		}

		public ExtensionManager RegExt(INoxExtension ext)
		{
			ExtList.Add(ext);
			return this;
		}

		public ExtensionManager RegExt<T>() where T : INoxExtension
		{
			T ext = Activator.CreateInstance<T>();
			ExtList.Add(ext);
			return this;
		}
		
		public ExtensionManager RegExt(Action<TcpClient> process)
		{
			var ext = new DummyExtension(process);
			ExtList.Add(ext);
			return this;
		}
		
		public void ProcessAllExt(TcpClient tcpClient)
		{
			var itor = ExtList.GetEnumerator();
			while (itor.MoveNext())
			{
				var ext = itor.Current;

				// if previous extension close the tcp stream/client,
				// the later extensions would be ignored
				if (tcpClient.Connected)
				{
					ext.Process(tcpClient);
				}
			}
		}
	}
}
