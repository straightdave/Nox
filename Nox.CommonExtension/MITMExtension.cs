using System;
using System.Net;
using Nox.Core.Extension;

namespace Nox.CommonExtension
{
	// Man-In-The-Middle who only does the relay
	public class MITMExtension : INoxExtension
	{
		public void Process(HttpListenerContext context)
		{
			Console.WriteLine($"Doing {context.Request.Url}");
		}
	}
}
