using System;
using System.Net;

namespace Nox.Core.Extension
{
	class DummyExtension : INoxExtension
	{
		private Action<HttpListenerContext> _action;

		public DummyExtension(Action<HttpListenerContext> process)
		{
			_action = process;
		}

		public void Process(HttpListenerContext context)
		{
			_action.Invoke(context);
		}
	}
}
