using System;
using System.IO;
using FluentFTP.GnuTLS.Core;
using FluentFTP.GnuTLS.Enums;

namespace FluentFTP.GnuTLS {

	internal partial class GnuTlsInternalStream : Stream, IDisposable {

		private void SetupClientCertificates() {

			//
			// TODO: Setup (if any) client certificates for verification
			//       by the server, at this point.
			// ****
			//

			Logging.LogGnuFunc(GnuMessage.Handshake, "Setup client certificate - currently not implemented");

		}

	}
}
