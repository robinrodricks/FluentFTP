using FluentFTP.GnuTLS.Core;

using System;
using System.IO;

namespace FluentFTP.GnuTLS {

	internal partial class GnuTlsStream : Stream, IDisposable {

		private void ReportClientCertificateUsed() {

			if (Native.CertificateClientGetRequestStatus(sess)) {
				Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, "Server requested client certificate");
			}
			else {
				Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, "Server did not request client certificate");
			}

		}

	}
}
