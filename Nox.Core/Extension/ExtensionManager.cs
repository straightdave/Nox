using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Core.Extension
{
	internal class ExtensionManager
	{
		public List<INoxExtension> ExtList { get; set; }

		public ExtensionManager()
		{
			ExtList = new List<INoxExtension>();
		}

		public ExtensionManager RegExt<T>() where T : INoxExtension
		{
			T ext = Activator.CreateInstance<T>();
			ext.Init();
			ExtList.Add(ext);
			return this;
		}

		public INoxExtension GetExt<T>(int n = 0) where T : INoxExtension
		{
			return ExtList.Where(e => e.GetType() == typeof(T)).ElementAtOrDefault(n);
		}

		public void ProcessAllExt(HttpListenerContext context)
		{
			for (int i = 0; i < ExtList.Count(); i++)
			{
				var ext = ExtList.ElementAt(i);
				ext.Process(context);
			}
		}
	}
}
