using Xunit;
using Xunit.Sdk;

namespace FluentFTP.Xunit.Attributes {
	[XunitTestCaseDiscoverer("FluentFTP.Xunit.Attributes.Internal.SkippableFactDiscoverer", "FluentFTP.Xunit")]
	public class SkippableFactAttribute : FactAttribute { }
}