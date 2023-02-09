using System;
using System.IO;
using FluentFTP.GnuTLS.Core;
using FluentFTP.GnuTLS.Enums;

namespace FluentFTP.GnuTLS {

	internal partial class GnuTlsInternalStream : Stream, IDisposable {

		private void ReportClientCertificateUsed() {

			if (Core.GnuTls.CertificateClientGetRequestStatus(sess)) {
				Logging.LogGnuFunc(GnuMessage.Handshake, "Server requested client certificate");
			}
			else {
				Logging.LogGnuFunc(GnuMessage.Handshake, "Server did not request client certificate");
			}

		}

	}
}
