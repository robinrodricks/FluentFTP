using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP.Xunit.Attributes {
	public class SkipTestException : Exception {
		public SkipTestException(string reason)
			: base(reason) { }
	}
}