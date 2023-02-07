using FluentFTP.Client.BaseClient;
using FluentFTP.Streams;

using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;

namespace FluentFTP.GnuTLS {

	public class FtpGnuTlsStream : IFtpStream {

		private GnuTlsStream BaseStream;

		public BaseFtpClient Client;

		public void Init(
			BaseFtpClient client,
			string targetHost,
			Socket socket,
			CustomRemoteCertificateValidationCallback customRemoteCertificateValidation,
			bool isControl,
			IFtpStream controlConnStream,
			IFtpStreamConfig config) {

			// use default config if not given or if wrong type
			if (config == null || (config as FtpGnuConfig) == null) {
				config = new FtpGnuConfig();
			}

			// link to client
			Client = client;

			var typedConfig = config as FtpGnuConfig;

			GnuTlsStream.GnuStreamLogCBFunc fluentFtpLog =
				s => ((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Verbose, "GnuTLS: " + s);

			// create a Gnu TLS stream
			BaseStream = new GnuTlsStream(
				targetHost,
				socket,
				customRemoteCertificateValidation,
				isControl ? "ftp" : "ftp-data",
				isControl ? null : (controlConnStream as FtpGnuTlsStream).BaseStream,
				typedConfig.Priority,
				typedConfig.HandshakeTimeout,
				fluentFtpLog,
				typedConfig.LogLevel,
				typedConfig.LogBuffSize);

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
			return GnuTlsStream.SslProtocol;
		}
		public string GetCipherSuite() {
			return GnuTlsStream.CipherSuite;
		}
		public void Dispose() {
			BaseStream?.Dispose();
			BaseStream = null;
		}

	}
}
