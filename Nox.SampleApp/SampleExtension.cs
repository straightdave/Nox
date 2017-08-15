using System.Net;
using System.Text;
using Nox.Core.Extension;

namespace Nox.SampleApp
{
	/// <summary>
	/// A crazy extension who merely responds with 'Hello World'
	/// </summary>
	class SampleExtension : INoxExtension
	{
		public void Process(HttpListenerContext context)
		{
			using (var ostream = context.Response.OutputStream)
			{
				var bytes = Encoding.UTF8.GetBytes("<html><body><h1>Hello World</h1></body></html>");
				ostream.Write(bytes, 0, bytes.Length);
			}
		}
	}
}
