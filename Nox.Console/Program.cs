using System;
using Nox.Core;
using Nox.CommonExtension;

namespace Nox.SampleApp
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Nox ...");

			new NoxServer()
				.ListenTo(12345)
				.RegisterExt(context => Console.WriteLine($"Get {context.Request.Url}"))
				.RegisterExt<MITMExtension>()
				.RegisterExt(context => Console.WriteLine($"Done {context.Request.Url}"))
				.Start();
		}
	}
}
