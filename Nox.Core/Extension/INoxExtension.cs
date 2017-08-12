using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Core.Extension
{
	public interface INoxExtension
	{
		void Init();

		void Process(HttpListenerContext context);
	}
}
