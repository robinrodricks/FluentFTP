using System;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Client.BaseClient;
using FluentFTP.Xunit.Docker;
using System.Net;

namespace FluentFTP.Tests.Integration.System {

	public enum UseStream : uint {
		SslStream,
		GnuTlsStream,
		BouncyCastleStream,
	}

	public class IntegrationTestSuite {

		protected readonly DockerFtpServer _fixture;
		protected readonly UseStream _stream;

		private static int _certificateValidations;
		private static int _hostnameMismatches;

		/// <summary>
		/// Number of server certificates the Bouncy Castle stream asked us to validate since the last reset.
		/// </summary>
		public static int CertificateValidations => _certificateValidations;

		/// <summary>
		/// Number of those validations that reported a hostname mismatch.
		/// </summary>
		public static int HostnameMismatches => _hostnameMismatches;

		public static void ResetCertificateCounters() {
			_certificateValidations = 0;
			_hostnameMismatches = 0;
		}

		public IntegrationTestSuite(DockerFtpServer fixture, UseStream stream) {
			_fixture = fixture;
			_stream = stream;
		}

		/// <summary>
		/// Main entrypoint executed for all types of FTP servers.
		/// </summary>
		public virtual void RunAllTests() {
		}

		/// <summary>
		/// Main entrypoint executed for all types of FTP servers.
		/// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async virtual Task RunAllTestsAsync() {
		}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

		/// <summary>
		/// Creates a new FTP client capable of connecting to this dockerized FTP server.
		/// </summary>
		protected FtpClient GetClient() {
			var client = new FtpClient("localhost", new NetworkCredential(_fixture.GetUsername(), _fixture.GetPassword()));
			Configure(client);
			return client;
		}

		/// <summary>
		/// Creates & Connects a new FTP client capable of connecting to this dockerized FTP server.
		/// </summary>
		protected FtpClient GetConnectedClient() {
			var client = GetClient();
			client.AutoConnect();
			return client;
		}

		/// <summary>
		/// Creates a new FTP client capable of connecting to this dockerized FTP server.
		/// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		protected async Task<AsyncFtpClient> GetAsyncClient() {
			var client = new AsyncFtpClient("localhost", new NetworkCredential(_fixture.GetUsername(), _fixture.GetPassword()));
			Configure(client);
			return client;
		}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

		/// <summary>
		/// Creates & Connects a new FTP client capable of connecting to this dockerized FTP server.
		/// </summary>
		protected async Task<AsyncFtpClient> GetConnectedAsyncClient() {
			var client = await GetAsyncClient();
			await client.AutoConnect();
			return client;
		}

		/// <summary>
		/// Applies the stream, encryption and certificate policy shared by the sync and async clients.
		/// </summary>
		private void Configure(BaseFtpClient client) {
			if (_stream == UseStream.GnuTlsStream) {
				client.Config.CustomStream = typeof(FluentFTP.GnuTLS.GnuTlsStream);
				client.Config.CustomStreamConfig = new FluentFTP.GnuTLS.GnuConfig();
			}
			else if (_stream == UseStream.BouncyCastleStream) {
				client.Config.CustomStream = typeof(FluentFTP.BouncyCastle.BouncyCastleFtpStream);
				client.Config.CustomStreamConfig = new FluentFTP.BouncyCastle.BouncyCastleFtpConfig();
			}
			client.Config.EncryptionMode = FtpEncryptionMode.Auto;
			if (_stream == UseStream.BouncyCastleStream) {
				// The docker servers use self-signed certificates that carry a SAN for localhost, so the
				// chain error is expected while a hostname error must fail the connection.
				client.ValidateCertificate += (_, args) => {
					Interlocked.Increment(ref _certificateValidations);
					var mismatch = args.PolicyErrorMessage.Contains("does not match", StringComparison.Ordinal);
					if (mismatch) {
						Interlocked.Increment(ref _hostnameMismatches);
					}
					args.Accept = !mismatch;
				};
			}
			else {
				client.Config.ValidateAnyCertificate = true;
			}
			client.Config.LogHost = true;
			client.Config.LogUserName = true;
			client.Config.LogPassword = true;
			// Lets a CI run capture the FTP conversation when a container test fails.
			client.Config.LogToConsole = string.Equals(Environment.GetEnvironmentVariable("FLUENTFTP_LOG_TO_CONSOLE"), "true", StringComparison.OrdinalIgnoreCase);
		}

	}
}
