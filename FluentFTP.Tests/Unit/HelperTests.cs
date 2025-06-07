using FluentFTP.Helpers;
using FluentFTP.Model.Functions;
using Xunit;

namespace FluentFTP.Tests.Unit {
	public class HelperTests {

		[Fact]
		public void ValuePrinterTest() {

			var obj = new FtpAutoDetectConfig();
			var txt = ValuePrinter.ObjectToString(obj);

			Assert.Equal("CloneConnection = True, FirstOnly = True, IncludeImplicit = True, AbortOnTimeout = True, RequireEncryption = False, ProtocolPriority = [Tls12]", txt);
		}

	}
}