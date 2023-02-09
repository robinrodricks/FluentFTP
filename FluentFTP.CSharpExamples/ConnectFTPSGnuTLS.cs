using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Client.BaseClient;
using FluentFTP.GnuTLS;
using FluentFTP.GnuTLS.Enums;

namespace Examples {
	internal static class ConnectFTPSGnuTLSExample {

		public static void ConnectFTPSCertificate() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {


				// enable GnuTLS streams for FTP client
				conn.Config.CustomStream = typeof(GnuTlsStream);
				conn.Config.CustomStreamConfig = new GnuConfig() {
					LogLevel = 1,

					// sample setting to use the default security suite
					SecuritySuite = GnuSuite.Normal,

					// sample setting to include all TLS protocols except for TLS 1.0 and TLS 1.1
					SecurityOptions = new List<GnuOption> {
						new GnuOption(GnuOperator.Include, GnuCommand.Protocol_All),
						new GnuOption(GnuOperator.Exclude, GnuCommand.Protocol_Tls10),
						new GnuOption(GnuOperator.Exclude, GnuCommand.Protocol_Tls11),
					},

					// no profile required
					SecurityProfile = GnuProfile.None,

					// sample special flags (this is not normally required)
					AdvancedOptions = new List<GnuAdvanced> {
						GnuAdvanced.CompatibilityMode
					},

					HandshakeTimeout = 5000,
				};


				// connect using Explicit FTPS with TLS 1.3
				conn.Config.EncryptionMode = FtpEncryptionMode.Explicit;
				conn.Connect();
			}
		}

		public static async Task ConnectFTPSCertificateAsync() {
			var token = new CancellationToken();
			using (var conn = new AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")) {


				// enable GnuTLS streams for FTP client
				conn.Config.CustomStream = typeof(GnuTlsStream);
				conn.Config.CustomStreamConfig = new GnuConfig() {
					LogLevel = 1,

					// sample setting to use the default security suite
					SecuritySuite = GnuSuite.Normal,

					// sample setting to include all TLS protocols except for TLS 1.0 and TLS 1.1
					SecurityOptions = new List<GnuOption> {
						new GnuOption(GnuOperator.Include, GnuCommand.Protocol_All),
						new GnuOption(GnuOperator.Exclude, GnuCommand.Protocol_Tls10),
						new GnuOption(GnuOperator.Exclude, GnuCommand.Protocol_Tls11),
					},

					// no profile required
					SecurityProfile = GnuProfile.None,

					// sample special flags (this is not normally required)
					AdvancedOptions = new List<GnuAdvanced> {
						GnuAdvanced.CompatibilityMode
					},

					HandshakeTimeout = 5000,
				};


				// connect using Explicit FTPS with TLS 1.3
				conn.Config.EncryptionMode = FtpEncryptionMode.Explicit;
				await conn.Connect(token);
			}
		}


	}
}