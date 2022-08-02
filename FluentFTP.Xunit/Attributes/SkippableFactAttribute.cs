using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace FluentFTP.Xunit.Attributes {
	[XunitTestCaseDiscoverer("FluentFTP.Xunit.Internal.SkippableFactDiscoverer", "FluentFTP.Tests.Integration")]
	public class SkippableFactAttribute : FactAttribute { }
}