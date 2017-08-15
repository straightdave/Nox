using System;
using System.Collections.Generic;
using System.Net;

namespace Nox.Core.Extension
{
	internal class ExtensionManager
	{
		public List<INoxExtension> ExtList { get; private set; }

		public ExtensionManager()
		{
			ExtList = new List<INoxExtension>();
		}

		public ExtensionManager RegExt<T>() where T : INoxExtension
		{
			T ext = Activator.CreateInstance<T>();
			ExtList.Add(ext);
			return this;
		}

		public ExtensionManager RegExt(Action<HttpListenerContext> process)
		{
			var ext = new DummyExtension(process);
			ExtList.Add(ext);
			return this;
		}
		
		public void ProcessAllExt(HttpListenerContext context)
		{
			var itor = ExtList.GetEnumerator();
			while (itor.MoveNext())
			{
				var ext = itor.Current;
				ext.Process(context);
			}

			context.Response.OutputStream.Flush();
			context.Response.OutputStream.Close();
		}
	}
}
