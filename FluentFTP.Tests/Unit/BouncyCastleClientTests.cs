using System;
using System.Collections.Generic;
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
using FluentFTP.Client.BaseClient;
using Xunit;

namespace FluentFTP.Tests.Unit {
	public class BouncyCastleClientTests {
		[Theory]
		[InlineData(false, "default")]
		[InlineData(true, "default")]
		[InlineData(false, "pin")]
		[InlineData(true, "pin")]
		[InlineData(false, "any")]
		[InlineData(true, "any")]
		[InlineData(false, "reject")]
		[InlineData(true, "reject")]
		public async Task FluentFtpValidationPolicyRunsBeforeLogin(bool asynchronous, string policy) {
			using var certificate = CreateServerCertificate();
			using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			try {
				var accepts = policy == "pin" || policy == "any";
				var commands = new List<string>();
				var server = Task.Run(() => ServeLogin(listener, certificate, commands, accepts, deadline.Token));
				var client = CreateClient(asynchronous, ((IPEndPoint)listener.LocalEndpoint).Port);
				client.Config.ValidateAnyCertificate = policy == "any";
				string? errors = null;
				if (policy == "pin" || policy == "reject") {
					client.ValidateCertificate += (_, args) => {
						errors = args.PolicyErrorMessage;
						args.Accept = policy == "pin" && certificate.GetCertHashString(HashAlgorithmName.SHA256) ==
							args.Certificate.GetCertHashString(HashAlgorithmName.SHA256);
					};
				}
				var failure = await ConnectAndDisconnect(client, deadline.Token);
				await server.WaitAsync(deadline.Token);
				if (accepts) {
					Assert.Null(failure);
					Assert.Contains("USER test-user", commands);
					Assert.Contains("PASS test-password", commands);
				}
				else {
					// The same exception type as FluentFTP's SslStream path, so existing handlers keep working.
					var rejection = Assert.IsType<AuthenticationException>(failure);
					Assert.Contains("certificate was rejected", rejection.Message);
					Assert.Empty(commands);
				}
				if (policy == "pin" || policy == "reject") {
					Assert.Contains("does not match", errors!);
					Assert.Contains("chain", errors!, StringComparison.OrdinalIgnoreCase);
				}
			}
			finally {
				listener.Stop();
			}
		}

		[Fact]
		public async Task DiagnosticsAndClientCertificateWarningReachFluentFtpLog() {
			using var certificate = CreateServerCertificate();
			using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			try {
				var commands = new List<string>();
				var server = Task.Run(() => ServeLogin(listener, certificate, commands, true, deadline.Token));
				var client = CreateClient(false, ((IPEndPoint)listener.LocalEndpoint).Port);
				client.Config.ValidateAnyCertificate = true;
				client.Config.ClientCertificates.Add(certificate);
				var log = new List<(FtpTraceLevel Level, string Message)>();
				client.LegacyLogger = (level, message) => {
					lock (log) {
						log.Add((level, message));
					}
				};
				Assert.Null(await ConnectAndDisconnect(client, deadline.Token));
				await server.WaitAsync(deadline.Token);
				Assert.Contains(log, entry => entry.Level == FtpTraceLevel.Warn &&
					entry.Message.Contains("Client certificates", StringComparison.Ordinal));
				Assert.Contains(log, entry => entry.Level == FtpTraceLevel.Verbose &&
					entry.Message.Contains("BouncyCastle: Control TLS session", StringComparison.Ordinal));
			}
			finally {
				listener.Stop();
			}
		}

		private static BaseFtpClient CreateClient(bool asynchronous, int port) {
			BaseFtpClient client = asynchronous
				? new AsyncFtpClient("127.0.0.1", "test-user", "test-password", port)
				: new FtpClient("127.0.0.1", "test-user", "test-password", port);
			client.Config.EncryptionMode = FtpEncryptionMode.Implicit;
			client.Config.CustomStream = typeof(BouncyCastleFtpStream);
			client.Config.CustomStreamConfig = new BouncyCastleFtpConfig();
			client.Config.ConnectTimeout = client.Config.ReadTimeout = 3000;
			return client;
		}

		private static X509Certificate2 CreateServerCertificate() {
			using var key = RSA.Create(2048);
			var request = new CertificateRequest("CN=untrusted.example.test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
			using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
			// Schannel needs a PFX-imported key for server authentication on Windows.
#if NET9_0_OR_GREATER
			return X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pfx), null);
#else
			return new X509Certificate2(generated.Export(X509ContentType.Pfx));
#endif
		}

		private static async Task ServeLogin(TcpListener listener, X509Certificate2 certificate,
			List<string> commands, bool accepts, CancellationToken token) {
			using var peer = await listener.AcceptTcpClientAsync(token);
			using var tls = new SslStream(peer.GetStream());
			try {
				await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions {
					ServerCertificate = certificate,
					EnabledSslProtocols = SslProtocols.Tls12,
				}, token);
			}
			catch (Exception error) when (!accepts && (error is AuthenticationException || error is IOException)) {
				return;
			}

			using var reader = new StreamReader(tls, Encoding.ASCII, false, 1024, true);
			using var writer = new StreamWriter(tls, Encoding.ASCII, 1024, true) { AutoFlush = true, NewLine = "\r\n" };
			await writer.WriteLineAsync("220 Loopback FTPS test");
			while (true) {
				var command = await reader.ReadLineAsync(token);
				if (command == null) {
					break;
				}
				commands.Add(command);
				var verb = command.Split(' ')[0];
				await writer.WriteLineAsync(GetResponse(verb));
				if (verb == "QUIT") {
					break;
				}
			}
		}

		private static string GetResponse(string verb) => verb switch {
			"USER" => "331 Password required",
			"PASS" => "230 Logged in",
			"SYST" => "215 UNIX Type: L8",
			"PWD" => "257 \"/\"",
			"FEAT" => "500 No features",
			"QUIT" => "221 Goodbye",
			_ => "200 OK",
		};

		private static async Task<Exception?> ConnectAndDisconnect(BaseFtpClient client, CancellationToken token) {
			if (client is AsyncFtpClient asyncClient) {
				using (asyncClient) {
					var failure = await Record.ExceptionAsync(() => asyncClient.Connect(token));
					if (failure == null) {
						await asyncClient.Disconnect(token);
					}
					return failure;
				}
			}

			using var syncClient = (FtpClient)client;
			var syncFailure = Record.Exception(() => syncClient.Connect());
			if (syncFailure == null) {
				syncClient.Disconnect();
			}
			return syncFailure;
		}
	}
}
