using FluentFTP.Xunit.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace FluentFTP.Xunit.Attributes.Internal {
	internal class SkippableFactTestCase : XunitTestCase {
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
		public SkippableFactTestCase() { }

		public SkippableFactTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod, object[] testMethodArguments = null)
			: base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments) { }

		public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
														IMessageBus messageBus,
														object[] constructorArguments,
														ExceptionAggregator aggregator,
														CancellationTokenSource cancellationTokenSource) {
			var skipMessageBus = new SkippableFactMessageBus(messageBus);
			RunSummary result;
			if (SkippableState.ShouldSkip) {
				/*
				 * This does skip execution, but does not register as "skipped" in the summary.
				 */
				result = new RunSummary {
					Total = 1,
					Skipped = 1,
				};
			}
			else {
				result = await base.RunAsync(diagnosticMessageSink, skipMessageBus, constructorArguments, aggregator, cancellationTokenSource);
			}


			if (skipMessageBus.DynamicallySkippedTestCount > 0) {
				result.Failed -= skipMessageBus.DynamicallySkippedTestCount;
				result.Skipped += skipMessageBus.DynamicallySkippedTestCount;
			}

			return result;
		}
	}
}