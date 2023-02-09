using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;
using FluentFTP.Client.BaseClient;
using FluentFTP.GnuTLS.Priority;
using FluentFTP.Streams;

namespace FluentFTP.GnuTLS {

	public class GnuTlsStream : IFtpStream {

		private GnuTlsInternalStream BaseStream;

		public BaseFtpClient Client;

		public void Init(
			BaseFtpClient client,
			string targetHost,
			Socket socket,
			CustomRemoteCertificateValidationCallback customRemoteCertificateValidation,
			bool isControl,
			IFtpStream controlConnStream,
			IFtpStreamConfig untypedConfig) {

			// use default config if not given or if wrong type
			if (untypedConfig == null || (untypedConfig as GnuConfig) == null) {
				untypedConfig = new GnuConfig();
			}

			// link to client
			Client = client;

			var config = untypedConfig as GnuConfig;

			GnuTlsInternalStream.GnuStreamLogCBFunc fluentFtpLog =
				s => ((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Verbose, "GnuTLS: " + s);

			// build the priority string
			var priority = PriorityBuilder.Build(
				config.SecuritySuite, config.SecurityOptions,
				config.AdvancedOptions, config.SecurityProfile);

			// create a Gnu TLS stream
			BaseStream = new GnuTlsInternalStream(
				targetHost,
				socket,
				customRemoteCertificateValidation,
				isControl ? "ftp" : "ftp-data",
				isControl ? null : (controlConnStream as GnuTlsStream).BaseStream,
				priority,
				config.HandshakeTimeout,
				fluentFtpLog,
				config.LogLevel,
				config.LogMessages,
				config.LogLength);

		}

		public Stream GetBaseStream() {
			return BaseStream;
		}
		public bool CanRead() {
			return BaseStream.CanRead;
		}
		public bool CanWrite() {
			return BaseStream.CanWrite;
		}
		public SslProtocols GetSslProtocol() {
			return GnuTlsInternalStream.SslProtocol;
		}
		public string GetCipherSuite() {
			return GnuTlsInternalStream.CipherSuite;
		}
		public void Dispose() {
			BaseStream?.Dispose();
			BaseStream = null;
		}

	}
}
