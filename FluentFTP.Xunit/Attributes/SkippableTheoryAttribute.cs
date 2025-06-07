using Xunit;
using Xunit.Sdk;

namespace FluentFTP.Xunit.Attributes {
	[XunitTestCaseDiscoverer("FluentFTP.Xunit.Attributes.Internal.SkippableTheoryDiscoverer", "FluentFTP.Xunit")]
	public class SkippableTheoryAttribute : TheoryAttribute { }
}