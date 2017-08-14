using System.Net;

namespace Nox.Core.Extension
{
	public interface INoxExtension
	{
		void Process(HttpListenerContext context);
	}
}
