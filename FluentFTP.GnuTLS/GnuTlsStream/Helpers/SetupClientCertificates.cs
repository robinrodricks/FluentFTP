using FluentFTP.GnuTLS.Core;

using System;
using System.IO;

namespace FluentFTP.GnuTLS {

	internal partial class GnuTlsStream : Stream, IDisposable {

		private void SetupClientCertificates() {

			//
			// TODO: Setup (if any) client certificates for verification
			//       by the server, at this point.
			// ****
			//

			Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, "Setup client certificate - currently not implemented");

		}

	}
}
