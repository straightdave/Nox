using System;
using System.Threading;
using Nox.Core;
using Nox.Extension;

namespace Nox.SampleApp
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Nox ...");

			var nox = new NoxServer()
				.RegisterExt<NoxProxy>()
				.Start();

			while (true)
			{
				if (Console.ReadKey().KeyChar.Equals('q')) break;
				Thread.Sleep(0);
			}

			Console.WriteLine("\r\nStopping Nox ...");
			nox.Stop();
		}
	}
}
