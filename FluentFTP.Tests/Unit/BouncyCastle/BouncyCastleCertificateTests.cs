using System;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.BouncyCastle;
using Org.BouncyCastle.Tls;
using Xunit;
using CertificateRequest = System.Security.Cryptography.X509Certificates.CertificateRequest;

namespace FluentFTP.Tests.Unit.BouncyCastle {
	public class BouncyCastleCertificateTests {
		private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";
		private const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";
		private const string SubjectAlternativeNameOid = "2.5.29.17";
		private const string CommonNameOid = "2.5.4.3";

		[Theory]
		[InlineData("ftp.example.test", "ftp.example.test", true)]
		[InlineData("ftp.example.test", "other.example.test", false)]
		[InlineData("FTP.EXAMPLE.TEST", "ftp.example.test", true)]
		[InlineData("ftp.example.test.", "ftp.example.test", true)]
		[InlineData("ftp.example.test", "ftp.example.test.", true)]
		[InlineData("a.example.test", "*.example.test", true)]
		[InlineData("a.b.example.test", "*.example.test", false)]
		[InlineData("example.test", "*.example.test", false)]
		[InlineData("ftp.example.test", "f*.example.test", false)]
		[InlineData("ftp.example.test.evil.test", "*.example.test", false)]
		[InlineData("bücher.example", "xn--bcher-kva.example", true)]
		[InlineData("127.0.0.1", "127.0.0.1", false)]
		public void DnsNamesMatchOnlyTheIntendedHost(string host, string dnsName, bool expected) {
			using var certificate = CreateCertificate("ignored.example.test", dnsName);
			Assert.Equal(expected, ServerCertificateValidation.MatchesHost(certificate, host));
		}

		[Theory]
		[InlineData("127.0.0.1", "127.0.0.1", true)]
		[InlineData("127.0.0.2", "127.0.0.1", false)]
		[InlineData("::1", "::1", true)]
		[InlineData("[::1]", "::1", true)]
		[InlineData("2001:db8::1", "2001:db8:0:0:0:0:0:1", true)]
		public void IpAddressesRequireMatchingIpAlternativeNames(string host, string ipName, bool expected) {
			using var certificate = CreateCertificate("ignored.example.test", ipName: ipName);
			Assert.Equal(expected, ServerCertificateValidation.MatchesHost(certificate, host));
		}

		[Theory]
		[InlineData("ftp.example.test", "other.example.test", false)]
		[InlineData("ftp.example.test", null, true)]
		[InlineData("*.example.test", null, false)]
		[InlineData("127.0.0.1", null, false)]
		public void CommonNameFallbackCannotOverrideSanOrMatchAnIp(string commonName, string? dnsName, bool expected) {
			using var certificate = CreateCertificate(commonName, dnsName);
			var host = commonName == "127.0.0.1" ? commonName : "ftp.example.test";
			Assert.Equal(expected, ServerCertificateValidation.MatchesHost(certificate, host));
		}

		[Theory]
		[InlineData("")]
		[InlineData(" ")]
		[InlineData("*.example.test")]
		[InlineData("ftp.example.test..")]
		[InlineData("https://example.test")]
		public void InvalidTargetHostsAreRejected(string host) {
			Assert.Throws<ArgumentException>(() => ServerCertificateValidation.NormalizeHost(host));
		}

		[Fact]
		public void MalformedSanFailsClosedEvenWithMatchingCommonName() {
			using var key = RSA.Create(2048);
			var request = NewRequest("ftp.example.test", key);
			request.CertificateExtensions.Add(new X509Extension(SubjectAlternativeNameOid, new byte[] { 0x30, 0x03, 0x82, 0x01 }, false));
			using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
			Assert.False(ServerCertificateValidation.MatchesHost(certificate, "ftp.example.test"));
		}

		[Theory]
		[InlineData("ftp.example.test..")]
		[InlineData("ftp.example.test\0.evil.test")]
		public void MalformedDnsAlternativeNamesCannotMatch(string dnsName) {
			using var key = RSA.Create(2048);
			var request = NewRequest("ftp.example.test", key);
			// Encode directly because SubjectAlternativeNameBuilder rejects these names.
			var writer = new AsnWriter(AsnEncodingRules.DER);
			using (writer.PushSequence()) {
				writer.WriteCharacterString(UniversalTagNumber.IA5String, dnsName, new Asn1Tag(TagClass.ContextSpecific, 2));
			}
			request.CertificateExtensions.Add(new X509Extension(SubjectAlternativeNameOid, writer.Encode(), false));
			using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
			Assert.False(ServerCertificateValidation.MatchesHost(certificate, "ftp.example.test"));
		}

		[Fact]
		public void CommonNameWithUnsupportedStringTypeCannotMatch() {
			using var key = RSA.Create(2048);
			var writer = new AsnWriter(AsnEncodingRules.DER);
			using (writer.PushSequence())
			using (writer.PushSetOf())
			using (writer.PushSequence()) {
				writer.WriteObjectIdentifier(CommonNameOid);
				// UniversalString "A": valid ASN.1, but not a string type that AsnReader can decode.
				writer.WriteEncodedValue(new byte[] { 0x1C, 0x04, 0x00, 0x00, 0x00, 0x41 });
			}
			var request = new CertificateRequest(new X500DistinguishedName(writer.Encode()), key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
			using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
			Assert.False(ServerCertificateValidation.MatchesHost(certificate, "ftp.example.test"));
		}

		[Theory]
		[InlineData("ftp.example.test", true)]
		[InlineData("other.example.test", false)]
		public void TrustedChainStillRequiresMatchingHostname(string host, bool expected) {
			using var certificate = CreateCertificate("unused.test", "ftp.example.test");
			using var chain = TrustOnly(certificate);
			var errors = ServerCertificateValidation.Validate(certificate, chain, host, false);
			Assert.Empty(chain.ChainStatus);
			Assert.Equal(expected, errors == string.Empty);
			if (!expected) {
				Assert.Contains("does not match", errors);
			}
		}

		[Fact]
		public void MatchingHostnameDoesNotBypassUntrustedChain() {
			using var certificate = CreateCertificate("unused.test", "ftp.example.test");
			using var unrelated = CreateCertificate("unrelated.test");
			using var chain = TrustOnly(unrelated);
			Assert.Contains("chain validation failed", ServerCertificateValidation.Validate(certificate, chain, "ftp.example.test", false));
		}

		[Fact]
		public void ClientAuthenticationOnlyCertificateIsRejected() {
			using var certificate = CreateCertificate("unused.test", "ftp.example.test", clientOnly: true);
			using var chain = TrustOnly(certificate);
			Assert.Contains("chain validation failed", ServerCertificateValidation.Validate(certificate, chain, "ftp.example.test", false));
		}

		[Theory]
		[InlineData(-3, -2)]
		[InlineData(2, 3)]
		public void CertificatesOutsideTheirValidityPeriodAreRejected(int startDays, int endDays) {
			using var key = RSA.Create(2048);
			var request = NewRequest("ftp.example.test", key);
			using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(startDays), DateTimeOffset.UtcNow.AddDays(endDays));
			using var chain = TrustOnly(certificate);
			Assert.NotEmpty(ServerCertificateValidation.Validate(certificate, chain, "ftp.example.test", false));
			Assert.Contains(chain.ChainStatus, status => status.Status.HasFlag(X509ChainStatusFlags.NotTimeValid));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void MissingRevocationInformationFailsClosedWhenEnabled(bool checkRevocation) {
			using var issuerKey = RSA.Create(2048);
			var request = NewRequest("Test CA " + Guid.NewGuid().ToString("N"), issuerKey);
			request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
			request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
			using var issuer = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(2));
			using var leafKey = RSA.Create(2048);
			using var leaf = NewRequest("ftp.example.test", leafKey).Create(issuer,
				DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), RandomNumberGenerator.GetBytes(16));
			using var chain = TrustOnly(issuer);
			var errors = ServerCertificateValidation.Validate(leaf, chain, "ftp.example.test", checkRevocation);
			Assert.Equal(checkRevocation, errors.Length > 0);
			if (checkRevocation) {
				Assert.Contains(chain.ChainStatus, status => status.Status.HasFlag(X509ChainStatusFlags.RevocationStatusUnknown));
			}
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task RevokedCertificateIsRejectedOnlyWhenRevocationEnabled(bool checkRevocation) {
			using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
			using var responder = new TestListener();
			responder.Start();
			var port = ((IPEndPoint)responder.LocalEndpoint).Port;
			using var issuerKey = RSA.Create(2048);
			var issuerRequest = NewRequest("Test CA " + Guid.NewGuid().ToString("N"), issuerKey);
			issuerRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
			issuerRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
			issuerRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(issuerRequest.PublicKey, false));
			using var issuer = issuerRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(2));
			using var leafKey = RSA.Create(2048);
			var leafRequest = NewRequest("ftp.example.test", leafKey);
			leafRequest.CertificateExtensions.Add(CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(
				new[] { $"http://127.0.0.1:{port}/{Guid.NewGuid():N}.crl" }));
			using var leaf = leafRequest.Create(issuer, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), RandomNumberGenerator.GetBytes(16));
			var crlBuilder = new CertificateRevocationListBuilder();
			crlBuilder.AddEntry(leaf, DateTimeOffset.UtcNow.AddHours(-1));
			var crl = crlBuilder.Build(issuer, 1, DateTimeOffset.UtcNow.AddDays(1), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1,
				DateTimeOffset.UtcNow.AddMinutes(-1));
			var response = checkRevocation ? ServeCrl(responder, crl, deadline.Token) : Task.CompletedTask;
			using var chain = TrustOnly(issuer);
			var errors = ServerCertificateValidation.Validate(leaf, chain, "ftp.example.test", checkRevocation);
			Assert.Equal(checkRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck, chain.ChainPolicy.RevocationMode);
			if (checkRevocation) {
				Assert.NotEmpty(errors);
				Assert.Contains(chain.ChainStatus, status => status.Status.HasFlag(X509ChainStatusFlags.Revoked));
			}
			else {
				Assert.Empty(errors);
			}
			await response;
		}

		[Theory]
		[InlineData("ftp.example.test", "ftp.example.test", false, false, true)]
		[InlineData("other.example.test", "other.example.test", true, true, true)]
		[InlineData("127.0.0.1", "", true, false, true)]
		[InlineData("bücher.example", "xn--bcher-kva.example", true, false, true)]
		[InlineData("other.example.test", "other.example.test", true, false, false)]
		[InlineData("ftp.example.test", "ftp.example.test", false, false, false)]
		public async Task HandshakeSendsSniAndReportsValidationErrorsToCallback(
			string host, string expectedSni, bool expectedNameError, bool checkRevocation, bool acceptErrors) {
			using var generated = CreateCertificate("unused.test", "ftp.example.test");
			using var certificate = ImportForServerAuthentication(generated);
			using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
			using var listener = new TestListener();
			listener.Start();
			string? sni = null;
			string? errors = null;
			X509Certificate? validated = null;
			X509RevocationMode? revocation = null;
			var server = ServeSniHandshake(listener, certificate, acceptErrors, name => sni = name, deadline.Token);
			using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.ReceiveTimeout = socket.SendTimeout = 10000;
			await socket.ConnectAsync(listener.LocalEndpoint, deadline.Token);
			using (var stream = new BouncyCastleFtpStream()) {
				using var client = new FtpClient();
				client.Config.ValidateCertificateRevocation = checkRevocation;
				var failure = Record.Exception(() => {
					stream.Init(client, host, socket, (_, presented, chain, message) => {
						validated = presented;
						errors = message;
						revocation = chain.ChainPolicy.RevocationMode;
						return acceptErrors || message.Length == 0;
					}, true, null!, new BouncyCastleFtpConfig());
				});
				AssertHandshakeOutcome(stream, failure, acceptErrors, certificate, validated);
				await server;
			}
			// SslStream decodes IDN SNI back to Unicode before invoking its callback.
			Assert.Equal(expectedSni, string.IsNullOrEmpty(sni) ? string.Empty : new IdnMapping().GetAscii(sni));
			Assert.NotNull(errors);
			Assert.Equal(expectedNameError, errors!.Contains("does not match", StringComparison.Ordinal));
			Assert.Equal(checkRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck, revocation);
		}

		[Fact]
		public async Task InitializationPreservesOriginalFailureWhenCloseAlsoThrows() {
			using var listener = new TestListener();
			listener.Start();
			using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			await socket.ConnectAsync(listener.LocalEndpoint);
			using var peer = await listener.AcceptTcpClientAsync();
			var original = new AuthenticationException("Original handshake failure");
			using var stream = new BouncyCastleFtpStream(network => new FailingProtocol(network, original));
			using var client = new FtpClient();
			var actual = Assert.Throws<AuthenticationException>(() => stream.Init(client, "localhost", socket,
				(_, _, _, _) => true, true, null!, new BouncyCastleFtpConfig()));
			Assert.Same(original, actual);
			Assert.False(stream.CanRead());
			Assert.False(stream.CanWrite());
			Assert.Throws<InvalidOperationException>(() => stream.GetBaseStream());
			stream.Dispose();
		}

		private static CertificateRequest NewRequest(string commonName, RSA key) => new CertificateRequest(
			"CN=" + commonName, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

		private static X509Certificate2 CreateCertificate(string commonName, string? dnsName = null, string? ipName = null, bool clientOnly = false) {
			using var key = RSA.Create(2048);
			var request = NewRequest(commonName, key);
			if (dnsName != null || ipName != null) {
				var san = new SubjectAlternativeNameBuilder();
				if (dnsName != null) { san.AddDnsName(dnsName); }
				if (ipName != null) { san.AddIpAddress(IPAddress.Parse(ipName)); }
				request.CertificateExtensions.Add(san.Build());
			}
			request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
				new OidCollection { new Oid(clientOnly ? ClientAuthenticationOid : ServerAuthenticationOid) }, false));
			return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
		}

		// Schannel needs a PFX-imported key for server authentication on Windows.
		// DefaultKeySet creates a temporary key, removed when the certificate is disposed.
		private static X509Certificate2 ImportForServerAuthentication(X509Certificate2 certificate) {
#if NET9_0_OR_GREATER
			return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), null);
#else
			return new X509Certificate2(certificate.Export(X509ContentType.Pfx));
#endif
		}

		private static X509Chain TrustOnly(X509Certificate2 certificate) {
			var chain = new X509Chain();
			chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
			chain.ChainPolicy.CustomTrustStore.Add(certificate);
			return chain;
		}

		private static Task ServeSniHandshake(TcpListener listener, X509Certificate2 certificate, bool acceptErrors,
			Action<string?> sniObserved, CancellationToken token) {
			return Task.Run(async () => {
				using var peer = await listener.AcceptTcpClientAsync(token);
				using var tls = new SslStream(peer.GetStream());
				try {
					await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions {
						EnabledSslProtocols = SslProtocols.Tls12,
						ServerCertificateSelectionCallback = (_, name) => { sniObserved(name); return certificate; },
					}, token);
					var buffer = new byte[1];
					await tls.ReadExactlyAsync(buffer, token);
					Assert.Equal(42, buffer[0]);
				}
				catch (Exception exception) when (!acceptErrors && (exception is AuthenticationException || exception is IOException)) {
					// A rejecting client closes the TLS handshake.
				}
			}, token);
		}

		private static void AssertHandshakeOutcome(BouncyCastleFtpStream stream, Exception? failure, bool acceptErrors,
			X509Certificate2 expected, X509Certificate? validated) {
			if (acceptErrors) {
				Assert.Null(failure);
				// The accepted certificate stays usable until the stream is disposed, as with SslStream.
				Assert.Equal(expected.GetCertHashString(), validated!.GetCertHashString());
				stream.GetBaseStream().WriteByte(42);
			}
			else {
				var rejection = Assert.IsType<AuthenticationException>(failure);
				Assert.Contains("certificate was rejected", rejection.Message);
				Assert.False(stream.CanWrite());
			}
		}

		private static async Task ServeCrl(TcpListener listener, byte[] body, CancellationToken token) {
			using var client = await listener.AcceptTcpClientAsync(token);
			using var stream = client.GetStream();
			using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
			string? line;
			do {
				line = await reader.ReadLineAsync(token);
			} while (!string.IsNullOrEmpty(line));
			var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/pkix-crl\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
			await stream.WriteAsync(headers, token);
			await stream.WriteAsync(body, token);
		}

		private sealed class TestListener : TcpListener, IDisposable {
			internal TestListener() : base(IPAddress.Loopback, 0) {
			}
			void IDisposable.Dispose() => Stop();
		}

		private sealed class FailingProtocol : TlsClientProtocol {
			private readonly Exception m_original;
			private readonly Stream m_network;
			internal FailingProtocol(Stream network, Exception original) : base(network) {
				m_network = network;
				m_original = original;
			}
			public override void Connect(TlsClient tlsClient) {
				_ = tlsClient;
				throw m_original;
			}
			public override void Close() {
				m_network.Dispose();
				throw new InvalidOperationException("Secondary cleanup failure");
			}
		}
	}
}
