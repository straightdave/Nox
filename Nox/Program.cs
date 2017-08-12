using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nox.Core;

namespace Nox
{
	class Program
	{
		static void Main(string[] args)
		{
			new Server()
				.ListenTo(12345)
				.RegisterExt<SimpleExt>()
				.Start();
		}
	}
}
