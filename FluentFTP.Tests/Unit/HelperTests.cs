using FluentFTP.Helpers;
using FluentFTP.Model.Functions;
using System.Text;
using Xunit;

namespace FluentFTP.Tests.Unit {
	public class HelperTests {

		[Fact]
		public void ValuePrinterTest() {

			var obj = new FtpAutoDetectConfig();
			var txt = ValuePrinter.ObjectToString(obj);

			Assert.Equal("CloneConnection = True, FirstOnly = True, IncludeImplicit = True, AbortOnTimeout = True, RequireEncryption = False, ProtocolPriority = [Tls12]", txt);
		}

		[Fact]
		public void EncodingToCode_UTF8() {
			var enc = Encoding.UTF8;
			var result = Encodings.ToCode(enc);
			Assert.Equal("System.Text.Encoding.UTF8", result);
		}

		[Fact]
		public void EncodingToCode_ASCII() {
			var enc = Encoding.ASCII;
			var result = Encodings.ToCode(enc);
			Assert.Equal("System.Text.Encoding.ASCII", result);
		}

		[Fact]
		public void EncodingToCode_GetEncoding() {
			var enc = Encoding.GetEncoding("iso-8859-1");
			var result = Encodings.ToCode(enc);
			Assert.Equal("System.Text.Encoding.GetEncoding(\"iso-8859-1\")", result);
		}

	}
}