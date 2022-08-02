using FluentFTP.Xunit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace FluentFTP.Xunit.Attributes.Internal {
	internal class SkippableFactMessageBus : IMessageBus {
		readonly IMessageBus innerBus;

		public SkippableFactMessageBus(IMessageBus innerBus) {
			this.innerBus = innerBus;
		}

		public int DynamicallySkippedTestCount { get; private set; }

		public void Dispose() { }

		public bool QueueMessage(IMessageSinkMessage message) {
			var testFailed = message as ITestFailed;
			if (testFailed != null) {
				var exceptionType = testFailed.ExceptionTypes.FirstOrDefault();
				if (exceptionType == typeof(SkipTestException).FullName) {
					DynamicallySkippedTestCount++;
					return innerBus.QueueMessage(new TestSkipped(testFailed.Test, testFailed.Messages.FirstOrDefault()));
				}
			}

			// Nothing we care about, send it on its way
			return innerBus.QueueMessage(message);
		}
	}
}