using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using FluentFTP.Client.BaseClient;
using FluentFTP.Streams;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace FluentFTP.BouncyCastle {
	/// <summary>
	/// Implements FluentFTP's custom stream contract with Bouncy Castle TLS and carries the control
	/// connection's resumable TLS 1.2 session into each FTPS data connection.
	/// </summary>
	public sealed class BouncyCastleFtpStream : IFtpStream, IDisposable {
		private TlsClientProtocol? m_protocol;
		private Stream? m_stream;
		private TlsSession? m_session;
		private ProtocolVersion? m_version;
		private int m_cipherSuite;
		private bool m_disposed;

		/// <inheritdoc />
		public void Init(
			BaseFtpClient client,
			string targetHost,
			Socket socket,
			CustomRemoteCertificateValidationCallback customRemoteCertificateValidation,
			bool isControl,
			IFtpStream controlConnStream,
			IFtpStreamConfig config) {
			if (m_disposed) {
				throw new ObjectDisposedException(nameof(BouncyCastleFtpStream));
			}

			var adapterConfig = config as BouncyCastleFtpConfig;
			if (adapterConfig == null) {
				throw new ArgumentException("Expected a BouncyCastleFtpConfig instance.", nameof(config));
			}

			var control = isControl ? null : controlConnStream as BouncyCastleFtpStream;
			if (!isControl && control == null) {
				throw new InvalidOperationException("The data connection did not receive its Bouncy Castle control stream.");
			}

			var sessionToResume = control?.m_session;
			if (!isControl && adapterConfig.RequireSessionResumption && sessionToResume == null) {
				throw new InvalidOperationException("The FTPS control connection did not provide a resumable TLS session.");
			}

			var networkStream = new NetworkStream(socket, false);
			m_protocol = new TlsClientProtocol(networkStream);
			var tlsClient = new ResumingTlsClient(
				client,
				sessionToResume,
				adapterConfig.AllowLegacyResumption,
				customRemoteCertificateValidation,
				adapterConfig.Diagnostic);

			try {
				m_protocol.Connect(tlsClient);
				m_stream = m_protocol.Stream;
				m_session = tlsClient.Context.ResumableSession;
				m_version = tlsClient.Context.SecurityParameters.NegotiatedVersion;
				m_cipherSuite = tlsClient.Context.SecurityParameters.CipherSuite;

				if (isControl) {
					adapterConfig.Diagnostic?.Invoke(m_session?.IsResumable == true
						? "Control TLS session is resumable."
						: "Control TLS session is not resumable.");
				}
				else {
					var resumed = tlsClient.Context.SecurityParameters.IsResumedSession;
					adapterConfig.Diagnostic?.Invoke(resumed
						? "Data connection resumed the control TLS session."
						: "Data connection completed without resuming the control TLS session.");

					if (adapterConfig.RequireSessionResumption && !resumed) {
						throw new AuthenticationException("The FTPS data connection did not resume the control TLS session.");
					}
				}
			}
			catch {
				Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public Stream GetBaseStream() {
			if (m_stream == null) {
				throw new InvalidOperationException("The TLS stream has not been initialized.");
			}

			return m_stream;
		}

		/// <inheritdoc />
		public bool CanRead() => m_stream?.CanRead == true;

		/// <inheritdoc />
		public bool CanWrite() => m_stream?.CanWrite == true;

		/// <inheritdoc />
		public SslProtocols GetSslProtocol() => m_version == ProtocolVersion.TLSv12
			? SslProtocols.Tls12
			: SslProtocols.None;

		/// <inheritdoc />
		public string GetCipherSuite() => $"0x{m_cipherSuite:X4}";

		/// <inheritdoc />
		public void Dispose() {
			if (m_disposed) {
				return;
			}

			m_disposed = true;
			try {
				m_protocol?.Close();
			}
			catch (IOException) {
				// FluentFTP owns the socket and may already have closed it.
			}
			finally {
				m_protocol = null;
				m_stream = null;
				m_session = null;
			}
		}

		private sealed class ResumingTlsClient : DefaultTlsClient {
			private readonly TlsSession? m_sessionToResume;
			private readonly bool m_allowLegacyResumption;
			private readonly object m_certificateValidationSender;
			private readonly CustomRemoteCertificateValidationCallback m_certificateValidation;
			private readonly Action<string>? m_diagnostic;

			public ResumingTlsClient(
				object certificateValidationSender,
				TlsSession? sessionToResume,
				bool allowLegacyResumption,
				CustomRemoteCertificateValidationCallback certificateValidation,
				Action<string>? diagnostic)
				: base(new BcTlsCrypto(new SecureRandom())) {
				m_certificateValidationSender = certificateValidationSender;
				m_sessionToResume = sessionToResume;
				m_allowLegacyResumption = allowLegacyResumption;
				m_certificateValidation = certificateValidation;
				m_diagnostic = diagnostic;
			}

			public TlsClientContext Context => m_context;

			public override TlsSession? GetSessionToResume() => m_sessionToResume;

			public override bool AllowLegacyResumption() => m_allowLegacyResumption;

			public override void NotifySessionToResume(TlsSession? session) {
				base.NotifySessionToResume(session);
				m_diagnostic?.Invoke(session == null
					? "No TLS session was offered for resumption."
					: $"Offered a resumable TLS session ({session.SessionID.Length}-byte ID).");
			}

			public override void NotifySessionID(byte[] sessionID) {
				base.NotifySessionID(sessionID);
				m_diagnostic?.Invoke($"Server selected a {sessionID.Length}-byte TLS session ID.");
			}

			protected override ProtocolVersion[] GetSupportedVersions() => new[] { ProtocolVersion.TLSv12 };

			public override TlsAuthentication GetAuthentication() =>
				new CertificateAuthentication(m_certificateValidationSender, m_certificateValidation, m_diagnostic);
		}

		private sealed class CertificateAuthentication : TlsAuthentication {
			private readonly object m_certificateValidationSender;
			private readonly CustomRemoteCertificateValidationCallback m_certificateValidation;
			private readonly Action<string>? m_diagnostic;

			public CertificateAuthentication(
				object certificateValidationSender,
				CustomRemoteCertificateValidationCallback certificateValidation,
				Action<string>? diagnostic) {
				m_certificateValidationSender = certificateValidationSender;
				m_certificateValidation = certificateValidation;
				m_diagnostic = diagnostic;
			}

			public TlsCredentials? GetClientCredentials(Org.BouncyCastle.Tls.CertificateRequest certificateRequest) {
				_ = certificateRequest;
				return null;
			}

			public void NotifyServerCertificate(TlsServerCertificate serverCertificate) {
				var certificateList = serverCertificate.Certificate.GetCertificateList();
				if (certificateList.Length == 0) {
					throw new AuthenticationException("The FTPS server did not provide a certificate.");
				}

				var certificates = certificateList
					.Select(item => LoadCertificate(item.GetEncoded()))
					.ToArray();

				try {
					using (var chain = new X509Chain()) {
						chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
						for (var index = 1; index < certificates.Length; index++) {
							chain.ChainPolicy.ExtraStore.Add(certificates[index]);
						}

						var chainValid = chain.Build(certificates[0]);
						var errorMessage = chainValid
							? string.Empty
							: string.Join("; ", chain.ChainStatus.Select(status => status.StatusInformation.Trim()));

						if (!m_certificateValidation(m_certificateValidationSender, certificates[0], chain, errorMessage)) {
							throw new AuthenticationException("The FTPS server certificate was rejected.");
						}
					}

					m_diagnostic?.Invoke("Server certificate accepted by FluentFTP validation policy.");
				}
				finally {
					foreach (var certificate in certificates) {
						certificate.Dispose();
					}
				}
			}

			private static X509Certificate2 LoadCertificate(byte[] encodedCertificate) {
#if NET9_0_OR_GREATER
				return X509CertificateLoader.LoadCertificate(encodedCertificate);
#else
#pragma warning disable SYSLIB0057
				return new X509Certificate2(encodedCertificate);
#pragma warning restore SYSLIB0057
#endif
			}
		}
	}
}
