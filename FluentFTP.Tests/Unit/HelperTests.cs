using FluentFTP.Helpers;
using FluentFTP.Model.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace FluentFTP.Tests.Unit {
	public class HelperTests {

		[Fact]
		public void ValuePrinterTest() {

			var obj = new FtpAutoDetectConfig();
			var txt = ValuePrinter.ObjectToString(obj);

			Assert.Equal(txt, "CloneConnection = True, FirstOnly = True, IncludeImplicit = True, AbortOnTimeout = True, RequireEncryption = False, ProtocolPriority = [Tls11, Tls12]");
		}

	}
}