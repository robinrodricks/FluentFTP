using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.BouncyCastle;
using FluentFTP.Streams;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using Org.BouncyCastle.Tls.Crypto;
using Xunit;
using CertificateRequest = System.Security.Cryptography.X509Certificates.CertificateRequest;

namespace FluentFTP.Tests.Unit.BouncyCastle {
	public class BouncyCastleTlsIntegrationTests {
		[Theory]
		[InlineData(true, true, false)]
		[InlineData(false, true, false)]
		[InlineData(false, false, false)]
		[InlineData(true, true, true)]
		public async Task DataConnectionsEnforceResumptionAndTransferExactBytes(bool resume, bool requireResume, bool legacy) {
			using var fixture = new TlsFixture { Resume = resume, Legacy = legacy };
			var config = new BouncyCastleFtpConfig { RequireSessionResumption = requireResume, AllowLegacyResumption = legacy };
			var diagnostics = new List<string>();
			config.Diagnostic = diagnostics.Add;
			using var control = new BouncyCastleFtpStream();
			await fixture.Exchange(control, null, config, (_, _, _, _) => true, true);
			Assert.False(fixture.LastResumed);
			Assert.Equal(SslProtocols.Tls12, control.GetSslProtocol());
			Assert.Equal("0xC02F", control.GetCipherSuite());
			for (var index = 0; index < 2; index++) {
				using var data = new BouncyCastleFtpStream();
				var succeeds = resume || !requireResume;
				var failure = await fixture.Exchange(data, control, config, (_, _, _, _) => true, succeeds);
				Assert.Equal(resume, fixture.LastResumed);
				if (!succeeds) {
					Assert.IsType<AuthenticationException>(failure);
					Assert.Contains("did not resume", failure!.Message);
					Assert.False(data.CanWrite());
					Assert.Throws<InvalidOperationException>(() => data.GetBaseStream());
				}
			}
			Assert.Equal(3, fixture.CompletedHandshakes);
			Assert.Equal(resume ? 2 : 0, diagnostics.Count(message => message == "Data connection resumed the control TLS session."));
		}

		[Fact]
		public async Task LegacySessionCannotResumeWithoutExplicitOptIn() {
			using var fixture = new TlsFixture { Legacy = true };
			using var control = new BouncyCastleFtpStream();
			await fixture.Exchange(control, null, new BouncyCastleFtpConfig(), (_, _, _, _) => true, true);
			using var data = new BouncyCastleFtpStream();
			var failure = await fixture.Exchange(data, control, new BouncyCastleFtpConfig(), (_, _, _, _) => true, false);
			Assert.IsType<AuthenticationException>(failure);
			Assert.False(fixture.LastResumed);
			Assert.False(data.CanWrite());
		}

		[Theory]
		[InlineData(1)]
		[InlineData(2)]
		[InlineData(4)]
		public async Task ServersWithoutTls12CannotNegotiate(int minorVersion) {
			using var fixture = new TlsFixture { Version = ProtocolVersion.Get(3, minorVersion) };
			using var stream = new BouncyCastleFtpStream();
			var failure = await fixture.Exchange(stream, null, new BouncyCastleFtpConfig(), (_, _, _, _) => true, false);
			var rejection = Assert.IsType<AuthenticationException>(failure);
			Assert.Contains("protocol_version", rejection.ToString());
			Assert.IsType<TlsException>(rejection.InnerException, exactMatch: false);
			Assert.False(stream.CanWrite());
			Assert.Equal(0, fixture.CompletedHandshakes);
		}

		// 0xC030 is the suite vsftpd's default configuration accepts; 0xC02C is its ECDSA-certificate
		// counterpart. Exchange() verifies a byte-exact 32769-byte round trip on every handshake.
		[Theory]
		[InlineData(CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384, "0xC030")]
		[InlineData(CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384, "0xC02C")]
		public async Task ServersLimitedToAes256GcmCanNegotiate(int suite, string expected) {
			using var fixture = new TlsFixture { CipherSuites = new[] { suite } };
			using var stream = new BouncyCastleFtpStream();
			await fixture.Exchange(stream, null, new BouncyCastleFtpConfig(), (_, _, _, _) => true, true);
			Assert.Equal(SslProtocols.Tls12, stream.GetSslProtocol());
			Assert.Equal(expected, stream.GetCipherSuite());
			for (var index = 0; index < 2; index++) {
				using var data = new BouncyCastleFtpStream();
				await fixture.Exchange(data, stream, new BouncyCastleFtpConfig(), (_, _, _, _) => true, true);
				Assert.True(fixture.LastResumed);
				Assert.Equal(expected, data.GetCipherSuite());
			}
			Assert.Equal(3, fixture.CompletedHandshakes);
		}

		// The AES-256 addition is deliberately limited to the two GCM suites above.
		[Theory]
		[InlineData(CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384)]
		[InlineData(CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384)]
		[InlineData(CipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384)]
		public async Task Aes256SuitesBeyondTheAddedGcmPairRemainUnoffered(int suite) {
			using var fixture = new TlsFixture { CipherSuites = new[] { suite } };
			using var stream = new BouncyCastleFtpStream();
			var failure = await fixture.Exchange(stream, null, new BouncyCastleFtpConfig(), (_, _, _, _) => true, false);
			Assert.IsType<AuthenticationException>(failure);
			Assert.False(stream.CanWrite());
			Assert.Equal(0, fixture.CompletedHandshakes);
		}

		[Theory]
		[InlineData(CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256, true)]
		[InlineData(CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA, true)]
		[InlineData(CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384, false)]
		[InlineData(CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256, false)]
		[InlineData(CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA, false)]
		public async Task StaticRsaCompatibilityDoesNotExpandBeyondOriginalDefaults(int suite, bool succeeds) {
			using var fixture = new TlsFixture { CipherSuites = new[] { suite } };
			using var stream = new BouncyCastleFtpStream();
			var failure = await fixture.Exchange(stream, null, new BouncyCastleFtpConfig(), (_, _, _, _) => true, succeeds);
			if (succeeds) {
				Assert.Equal($"0x{suite:X4}", stream.GetCipherSuite());
			}
			else {
				Assert.IsType<AuthenticationException>(failure);
				Assert.False(stream.CanWrite());
				Assert.Equal(0, fixture.CompletedHandshakes);
			}
		}

		[Fact]
		public async Task RejectedCertificateCannotExposeApplicationStream() {
			using var fixture = new TlsFixture();
			using var control = new BouncyCastleFtpStream();
			string? errors = null;
			var failure = await fixture.Exchange(control, null, new BouncyCastleFtpConfig(), (_, _, _, message) => {
				errors = message;
				return string.IsNullOrEmpty(message);
			}, false);
			// Callers see the SslStream exception type; the Bouncy Castle alert stays available underneath.
			var rejection = Assert.IsType<AuthenticationException>(failure);
			Assert.Contains("certificate was rejected", rejection.Message);
			var alert = Assert.IsType<TlsFatalAlert>(rejection.InnerException);
			Assert.Equal(AlertDescription.bad_certificate, alert.AlertDescription);
			Assert.NotNull(fixture.LastAlertReceived);
			Assert.Equal(AlertDescription.bad_certificate, fixture.LastAlertReceived.Value);
			Assert.Contains("chain", errors!, StringComparison.OrdinalIgnoreCase);
			Assert.Equal(0, fixture.CompletedHandshakes);
			Assert.False(control.CanRead());
			Assert.False(control.CanWrite());
		}

		[Fact]
		public async Task HandshakeReportsHostnameErrorEvenWhenChainIsAlsoUntrusted() {
			using var fixture = new TlsFixture();
			using var control = new BouncyCastleFtpStream();
			string? errors = null;
			await fixture.Exchange(control, null, new BouncyCastleFtpConfig(), (_, _, _, message) => {
				errors = message;
				return true; // Inspect both errors without modifying the OS trust store.
			}, true, "other.example.test");
			Assert.Contains("does not match", errors!);
			Assert.Contains("chain", errors!, StringComparison.OrdinalIgnoreCase);
		}

		private sealed class TlsFixture : IDisposable {
			private readonly RSA m_key = RSA.Create(2048);
			private readonly ECDsa m_ecdsaKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
			private readonly X509Certificate2 m_certificate;
			private readonly X509Certificate2 m_ecdsaCertificate;
			private readonly CancellationTokenSource m_deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
			private TlsSession? m_session;
			internal bool Resume { get; set; } = true;
			internal bool Legacy { get; set; }
			internal ProtocolVersion Version { get; set; } = ProtocolVersion.TLSv12;
			internal int[] CipherSuites { get; set; } = {
				CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
				CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA,
			};
			internal bool LastResumed { get; private set; }
			internal short? LastAlertReceived { get; private set; }
			internal int CompletedHandshakes { get; private set; }

			internal TlsFixture() {
				var san = new SubjectAlternativeNameBuilder();
				san.AddDnsName("ftp.example.test");
				var request = new CertificateRequest("CN=ftp.example.test", m_key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
				request.CertificateExtensions.Add(san.Build());
				m_certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

				// A second certificate so ECDHE_ECDSA suites can be negotiated; Bouncy Castle picks
				// between the two by the negotiated key exchange, as a real dual-certificate server does.
				var ecdsaRequest = new CertificateRequest("CN=ftp.example.test", m_ecdsaKey, HashAlgorithmName.SHA256);
				ecdsaRequest.CertificateExtensions.Add(san.Build());
				m_ecdsaCertificate = ecdsaRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
			}

			internal async Task<Exception?> Exchange(BouncyCastleFtpStream stream, BouncyCastleFtpStream? control,
				BouncyCastleFtpConfig config, CustomRemoteCertificateValidationCallback validation, bool succeeds,
				string host = "ftp.example.test") {
				var listener = new TcpListener(IPAddress.Loopback, 0);
				listener.Start();
				try {
					var payload = RandomNumberGenerator.GetBytes(32769); // Cross TLS record boundaries.
					LastResumed = false;
					LastAlertReceived = null;
					var server = Task.Run(() => ServeExchange(listener, payload, succeeds));
					using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					socket.ReceiveTimeout = socket.SendTimeout = 5000;
					await socket.ConnectAsync(listener.LocalEndpoint, m_deadline.Token);
					using var client = new FtpClient();
					var failure = Record.Exception(() => stream.Init(client, host, socket, validation, control == null, control!, config));
					if (succeeds && failure == null) {
						var tls = stream.GetBaseStream();
						await tls.WriteAsync(payload, m_deadline.Token);
						var received = new byte[payload.Length];
						await tls.ReadExactlyAsync(received, m_deadline.Token);
						Assert.Equal(payload, received);
					}
					await server.WaitAsync(m_deadline.Token);
					if (succeeds) { Assert.Null(failure); }
					else { Assert.NotNull(failure); }
					return failure;
				}
				finally {
					listener.Stop();
				}
			}

			private async Task ServeExchange(TcpListener listener, byte[] payload, bool succeeds) {
				using var peer = await listener.AcceptTcpClientAsync(m_deadline.Token);
				peer.ReceiveTimeout = peer.SendTimeout = 5000;
				var protocol = new TlsServerProtocol(peer.GetStream());
				try {
					protocol.Accept(new TestServer(this));
					EchoPayload(protocol.Stream, payload, succeeds);
				}
				catch (TlsFatalAlertReceived alert) when (!succeeds) {
					LastAlertReceived = alert.AlertDescription;
				}
				catch (IOException) when (!succeeds) {
					// A client rejecting the handshake sends an alert and closes TLS.
				}
				finally {
					try {
						protocol.Close();
					}
					catch (IOException) when (!succeeds) {
						// A rejecting peer may have already closed the underlying connection.
					}
				}
			}

			private static void EchoPayload(Stream stream, byte[] payload, bool succeeds) {
				var received = new byte[payload.Length];
				var count = 0;
				while (count < received.Length) {
					var read = stream.Read(received, count, received.Length - count);
					if (read == 0) {
						break;
					}
					count += read;
				}
				if (succeeds) {
					Assert.Equal(payload.Length, count);
					Assert.Equal(payload, received);
					stream.Write(received, 0, received.Length);
				}
				else {
					Assert.Equal(0, count);
				}
			}

			public void Dispose() {
				m_deadline.Dispose();
				m_session?.Invalidate();
				m_certificate.Dispose();
				m_key.Dispose();
				m_ecdsaCertificate.Dispose();
				m_ecdsaKey.Dispose();
			}

			private sealed class TestServer : DefaultTlsServer {
				private readonly TlsFixture m_fixture;
				internal TestServer(TlsFixture fixture) : base(new BcTlsCrypto(new SecureRandom())) { m_fixture = fixture; }
				protected override ProtocolVersion[] GetSupportedVersions() => new[] { m_fixture.Version };
				protected override int[] GetSupportedCipherSuites() => m_fixture.CipherSuites;
				public override bool ShouldUseExtendedMasterSecret() => !m_fixture.Legacy;
				public override TlsSession? GetSessionToResume(byte[] sessionID) =>
					m_fixture.Resume && m_fixture.m_session != null && sessionID.SequenceEqual(m_fixture.m_session.SessionID)
						? m_fixture.m_session : null;
				public override byte[] GetNewSessionID() => RandomNumberGenerator.GetBytes(32);
				public override void NotifyHandshakeComplete() {
					base.NotifyHandshakeComplete();
					m_fixture.LastResumed = m_context.SecurityParameters.IsResumedSession;
					m_fixture.CompletedHandshakes++;
					m_fixture.m_session ??= m_context.ResumableSession;
				}
				protected override TlsCredentialedDecryptor GetRsaEncryptionCredentials() => new BcDefaultTlsCredentialedDecryptor(
					(BcTlsCrypto)Crypto,
					new Org.BouncyCastle.Tls.Certificate(new[] { Crypto.CreateCertificate(m_fixture.m_certificate.RawData) }),
					PrivateKeyFactory.CreateKey(m_fixture.m_key.ExportPkcs8PrivateKey()));
				protected override TlsCredentialedSigner GetRsaSignerCredentials() => new BcDefaultTlsCredentialedSigner(
					new TlsCryptoParameters(m_context), (BcTlsCrypto)Crypto,
					PrivateKeyFactory.CreateKey(m_fixture.m_key.ExportPkcs8PrivateKey()),
					new Org.BouncyCastle.Tls.Certificate(new[] { Crypto.CreateCertificate(m_fixture.m_certificate.RawData) }),
					new SignatureAndHashAlgorithm(Org.BouncyCastle.Tls.HashAlgorithm.sha256, SignatureAlgorithm.rsa));
				protected override TlsCredentialedSigner GetECDsaSignerCredentials() => new BcDefaultTlsCredentialedSigner(
					new TlsCryptoParameters(m_context), (BcTlsCrypto)Crypto,
					PrivateKeyFactory.CreateKey(m_fixture.m_ecdsaKey.ExportPkcs8PrivateKey()),
					new Org.BouncyCastle.Tls.Certificate(new[] { Crypto.CreateCertificate(m_fixture.m_ecdsaCertificate.RawData) }),
					new SignatureAndHashAlgorithm(Org.BouncyCastle.Tls.HashAlgorithm.sha256, SignatureAlgorithm.ecdsa));
			}
		}
	}
}
