using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace FluentFTP.Tests.Integration.Skippable
{
	[XunitTestCaseDiscoverer("FluentFTP.Tests.Integration.Skippable.XunitExtensions.SkippableTheoryDiscoverer", "FluentFTP.Tests.Integration")]
	public class SkippableTheoryAttribute : TheoryAttribute { }
}
