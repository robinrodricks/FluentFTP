using FluentFTP.Xunit.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace FluentFTP.Xunit.Attributes.Internal {
	internal class SkippableFactDiscoverer : IXunitTestCaseDiscoverer {
		readonly IMessageSink diagnosticMessageSink;

		public SkippableFactDiscoverer(IMessageSink diagnosticMessageSink) {
			this.diagnosticMessageSink = diagnosticMessageSink;
		}

		public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute) {
			yield return new SkippableFactTestCase(diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod);
		}
	}
}