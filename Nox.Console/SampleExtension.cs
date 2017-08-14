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
			var response = context.Response;

			using (var ostream = response.OutputStream)
			{
				if (ostream.CanWrite)
				{
					var bytes = ReadString("<html><body><h1>Hello World</h1></body></html>");
					ostream.Write(bytes, 0, bytes.Length);
				}
			}
		}

		private byte[] ReadString(string msg)
		{
			return Encoding.UTF8.GetBytes(msg);
		}
	}
}
