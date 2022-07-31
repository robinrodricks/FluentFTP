using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP.Tests.Integration.Skippable
{
	public class SkipTestException : Exception
	{
		public SkipTestException(string reason)
			: base(reason) { }
	}
}
