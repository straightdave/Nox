using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Nox.Core.Extension;

namespace Nox
{
	class SimpleExt : INoxExtension
	{
		public void Init()
		{
			
		}

		public void Process(HttpListenerContext context)
		{
			var origin_req = context.Request;
			var new_req = HttpWebRequest.CreateDefault(origin_req.Url);

			Console.WriteLine(new_req.RequestUri.ToString());


			var hresp = (HttpWebResponse)new_req.GetResponse();


			var resp = context.Response;
			var ostream = resp.OutputStream;
			
			var hs = hresp.GetResponseStream();
			hs.CopyTo(ostream);

			hs.Close();
			ostream.Close();
		}

		private byte[] ReadString(string msg)
		{
			return Encoding.UTF8.GetBytes(msg);
		}
	}
}
