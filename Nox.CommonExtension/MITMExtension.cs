using System.Net;
using Nox.Core.Extension;
using System.Collections.Specialized;
using System;
using System.IO;

namespace Nox.CommonExtension
{
	public class MITMExtension : INoxExtension
	{
		public void Process(HttpListenerContext context)
		{
			var _req = CreateWebRequest(context.Request);
			var _resp = _req.GetResponse() as HttpWebResponse;

			Console.WriteLine("Got response");

			CopyWebResponse(context.Response, _resp);
		}

		private HttpWebRequest CreateWebRequest(HttpListenerRequest originRequest)
		{
			var _req = (HttpWebRequest)WebRequest.Create(originRequest.Url);

			_req.Host = originRequest.UserHostName;
			_req.UserAgent = originRequest.UserAgent;
			_req.Accept = originRequest.Headers["Accept"];
			_req.ContentType = originRequest.ContentType;
			_req.Proxy = null;

			if (originRequest.HasEntityBody)
			{
				using (var reqStream = _req.GetRequestStream())
				{
					var buff = new byte[1024];
					int read = 0;

					using (var ostream = originRequest.InputStream)
					{
						while ((read = ostream.Read(buff, 0, buff.Length)) > 0)
						{
							reqStream.Write(buff, 0, read);
						}
					}
				}
			}
			return _req;
		}

		private void CopyWebResponse(HttpListenerResponse proxyResponse, HttpWebResponse webResponse)
		{
			proxyResponse.StatusCode = (int)webResponse.StatusCode;
			proxyResponse.ContentType = webResponse.ContentType;
			proxyResponse.StatusDescription = webResponse.StatusDescription;
			
			using (var s = webResponse.GetResponseStream())
			{
				var buff = new byte[1024];
				int read = 0;

				using (var os = proxyResponse.OutputStream)
				{
					while ((read = s.Read(buff, 0, buff.Length)) > 0)
					{
						os.Write(buff, 0, read);
					}
				}					
			}
		}
	}
}
