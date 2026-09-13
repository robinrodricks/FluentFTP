using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
		private const string LogPrefix = "BouncyCastle: ";

		private readonly Func<Stream, TlsClientProtocol> m_protocolFactory;
		private TlsClientProtocol? m_protocol;
		private Stream? m_stream;
		private TlsSession? m_session;
		private X509Certificate2? m_serverCertificate;
		private ProtocolVersion? m_version;
		private int m_cipherSuite;
		private bool m_disposed;

		/// <summary>Creates a Bouncy Castle FTPS stream.</summary>
		public BouncyCastleFtpStream() : this(stream => new TlsClientProtocol(stream)) {
		}

		internal BouncyCastleFtpStream(Func<Stream, TlsClientProtocol> protocolFactory) {
			m_protocolFactory = protocolFactory;
		}

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

			var adapterConfig = config as BouncyCastleFtpConfig ?? throw new ArgumentException("Expected a BouncyCastleFtpConfig instance.", nameof(config));
			var sessionToResume = ResolveSessionToResume(isControl, controlConnStream, adapterConfig);
			var logger = (IInternalFtpClient)client;
			var diagnostic = CreateDiagnostic(logger, adapterConfig);
			if (isControl && client.Config.ClientCertificates.Count > 0) {
				logger.LogStatus(FtpTraceLevel.Warn, LogPrefix + "Client certificates are configured but FluentFTP.BouncyCastle does not support them; the server will not receive a client certificate.");
			}

			var normalizedHost = ServerCertificateValidation.NormalizeHost(targetHost);
			var authentication = new CertificateAuthentication(
				client,
				normalizedHost,
				client.Config.ValidateCertificateRevocation,
				customRemoteCertificateValidation,
				diagnostic,
				certificate => m_serverCertificate = certificate);
			var tlsClient = new ResumingTlsClient(authentication, normalizedHost, sessionToResume, adapterConfig.AllowLegacyResumption, diagnostic);

			try {
				m_protocol = CreateProtocol(socket);
				m_protocol.Connect(tlsClient);
				m_stream = m_protocol.Stream;
				m_session = tlsClient.Context.ResumableSession;
				m_version = tlsClient.Context.SecurityParameters.NegotiatedVersion;
				m_cipherSuite = tlsClient.Context.SecurityParameters.CipherSuite;

				ValidateSessionResumption(isControl, adapterConfig, tlsClient, diagnostic);
			}
			catch (Exception ex) {
				DisposeQuietly();
				if (ex is TlsException tlsException) {
					throw CreateAuthenticationException(tlsException);
				}
				throw;
			}
		}

		private static TlsSession? ResolveSessionToResume(bool isControl, IFtpStream controlConnStream, BouncyCastleFtpConfig config) {
			if (isControl) {
				return null;
			}

			var control = controlConnStream as BouncyCastleFtpStream
				?? throw new InvalidOperationException("The data connection did not receive its Bouncy Castle control stream.");
			if (config.RequireSessionResumption && control.m_session == null) {
				throw new InvalidOperationException("The FTPS control connection did not provide a resumable TLS session.");
			}

			return control.m_session;
		}

		private static Action<string> CreateDiagnostic(IInternalFtpClient logger, BouncyCastleFtpConfig config) {
			return message => {
				logger.LogStatus(FtpTraceLevel.Verbose, LogPrefix + message);
				config.Diagnostic?.Invoke(message);
			};
		}

		private TlsClientProtocol CreateProtocol(Socket socket) {
			var networkStream = new NetworkStream(socket, false);
			try {
				return m_protocolFactory(networkStream);
			}
			catch {
				networkStream.Dispose();
				throw;
			}
		}

		private void ValidateSessionResumption(bool isControl, BouncyCastleFtpConfig config, ResumingTlsClient tlsClient, Action<string> diagnostic) {
			if (isControl) {
				diagnostic(m_session?.IsResumable == true
					? "Control TLS session is resumable."
					: "Control TLS session is not resumable.");
				return;
			}

			var resumed = tlsClient.Context.SecurityParameters.IsResumedSession;
			diagnostic(resumed
				? "Data connection resumed the control TLS session."
				: "Data connection completed without resuming the control TLS session.");
			if (config.RequireSessionResumption && !resumed) {
				throw new AuthenticationException("The FTPS data connection did not resume the control TLS session.");
			}
		}

		private void DisposeQuietly() {
			try {
				Dispose();
			}
			catch (Exception) {
				// Cleanup must not replace the original initialization failure.
			}
		}

		// FluentFTP and its callers expect TLS failures as AuthenticationException, as thrown by SslStream.
		// The Bouncy Castle exception stays attached so the alert description remains available.
		private static AuthenticationException CreateAuthenticationException(TlsException exception) {
			var message = exception.InnerException is AuthenticationException cause
				? cause.Message
				: "The TLS handshake failed: " + exception.Message;
			return new AuthenticationException(message, exception);
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
				m_serverCertificate?.Dispose();
				m_serverCertificate = null;
			}
		}

		private sealed class ResumingTlsClient : DefaultTlsClient {
			private readonly TlsAuthentication m_authentication;
			private readonly string m_targetHost;
			private readonly TlsSession? m_sessionToResume;
			private readonly bool m_allowLegacyResumption;
			private readonly Action<string> m_diagnostic;

			public ResumingTlsClient(
				TlsAuthentication authentication,
				string targetHost,
				TlsSession? sessionToResume,
				bool allowLegacyResumption,
				Action<string> diagnostic)
				: base(new BcTlsCrypto(new SecureRandom())) {
				m_authentication = authentication;
				m_targetHost = targetHost;
				m_sessionToResume = sessionToResume;
				m_allowLegacyResumption = allowLegacyResumption;
				m_diagnostic = diagnostic;
			}

			public TlsClientContext Context => m_context;

			public override TlsSession? GetSessionToResume() => m_sessionToResume;

			public override bool AllowLegacyResumption() => m_allowLegacyResumption;

			public override void NotifySessionToResume(TlsSession? session) {
				base.NotifySessionToResume(session);
				m_diagnostic(session == null
					? "No TLS session was offered for resumption."
					: $"Offered a resumable TLS session ({session.SessionID.Length}-byte ID).");
			}

			public override void NotifySessionID(byte[] sessionID) {
				base.NotifySessionID(sessionID);
				m_diagnostic($"Server selected a {sessionID.Length}-byte TLS session ID.");
			}

			protected override ProtocolVersion[] GetSupportedVersions() => new[] { ProtocolVersion.TLSv12 };

			// Preserve the original adapter's cipher compatibility and add only the suite needed by vsftpd.
			protected override int[] GetSupportedCipherSuites() => TlsUtilities.GetSupportedCipherSuites(Crypto,
				new[] { CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384 }.Concat(base.GetSupportedCipherSuites()).ToArray());

			protected override IList<ServerName>? GetSniServerNames() => IPAddress.TryParse(m_targetHost, out _)
				? null
				: new[] { new ServerName(NameType.host_name, Encoding.ASCII.GetBytes(m_targetHost)) };

			public override TlsAuthentication GetAuthentication() => m_authentication;
		}

		private sealed class CertificateAuthentication : TlsAuthentication {
			private readonly object m_certificateValidationSender;
			private readonly string m_targetHost;
			private readonly bool m_checkRevocation;
			private readonly CustomRemoteCertificateValidationCallback m_certificateValidation;
			private readonly Action<string> m_diagnostic;
			private readonly Action<X509Certificate2> m_serverCertificateAccepted;

			public CertificateAuthentication(
				object certificateValidationSender,
				string targetHost,
				bool checkRevocation,
				CustomRemoteCertificateValidationCallback certificateValidation,
				Action<string> diagnostic,
				Action<X509Certificate2> serverCertificateAccepted) {
				m_certificateValidationSender = certificateValidationSender;
				m_targetHost = targetHost;
				m_checkRevocation = checkRevocation;
				m_certificateValidation = certificateValidation;
				m_diagnostic = diagnostic;
				m_serverCertificateAccepted = serverCertificateAccepted;
			}

			public TlsCredentials? GetClientCredentials(Org.BouncyCastle.Tls.CertificateRequest certificateRequest) {
				_ = certificateRequest;
				return null;
			}

			public void NotifyServerCertificate(TlsServerCertificate serverCertificate) {
				var certificateList = serverCertificate.Certificate.GetCertificateList();
				if (certificateList.Length == 0) {
					throw Reject("The FTPS server did not provide a certificate.");
				}

				var certificates = certificateList
					.Select(item => LoadCertificate(item.GetEncoded()))
					.ToArray();

				var accepted = false;
				try {
					accepted = Validate(certificates);
					if (!accepted) {
						throw Reject("The FTPS server certificate was rejected.");
					}

					// The accepted leaf certificate stays alive until the stream is disposed, as with SslStream.
					m_serverCertificateAccepted(certificates[0]);
					m_diagnostic("Server certificate accepted by FluentFTP validation policy.");
				}
				finally {
					for (var index = accepted ? 1 : 0; index < certificates.Length; index++) {
						certificates[index].Dispose();
					}
				}
			}

			private bool Validate(X509Certificate2[] certificates) {
				using (var chain = new X509Chain()) {
					for (var index = 1; index < certificates.Length; index++) {
						chain.ChainPolicy.ExtraStore.Add(certificates[index]);
					}

					var errorMessage = ServerCertificateValidation.Validate(
						certificates[0], chain, m_targetHost, m_checkRevocation);

					return m_certificateValidation(m_certificateValidationSender, certificates[0], chain, errorMessage);
				}
			}

			// A TlsFatalAlert makes Bouncy Castle send bad_certificate to the server instead of internal_error.
			private static TlsFatalAlert Reject(string message) =>
				new TlsFatalAlert(AlertDescription.bad_certificate, new AuthenticationException(message));

			private static X509Certificate2 LoadCertificate(byte[] encodedCertificate) {
#if NET9_0_OR_GREATER
				return X509CertificateLoader.LoadCertificate(encodedCertificate);
#else
				return new X509Certificate2(encodedCertificate);
#endif
			}
		}
	}
}
