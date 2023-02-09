using System.Threading.Tasks;
using FluentFTP.Xunit.Docker;
using System.Net;

namespace FluentFTP.Tests.Integration.System {

	public enum UseStream : uint {
		SslStream,
		GnuTlsStream, 
	}

	public class IntegrationTestSuite {

		protected readonly DockerFtpServer _fixture;
		protected readonly UseStream _stream;

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
		public async virtual Task RunAllTestsAsync() {
		}

		/// <summary>
		/// Creates a new FTP client capable of connecting to this dockerized FTP server.
		/// </summary>
		protected FtpClient GetClient() {
			var client = new FtpClient("localhost", new NetworkCredential(_fixture.GetUsername(), _fixture.GetPassword()));
			if (_stream == UseStream.GnuTlsStream) {
				client.Config.CustomStream = typeof(FluentFTP.GnuTLS.GnuTlsStream);
				client.Config.CustomStreamConfig = new FluentFTP.GnuTLS.GnuConfig();
			}
			client.Config.EncryptionMode = FtpEncryptionMode.Auto;
			client.Config.ValidateAnyCertificate = true;
			client.Config.LogHost = true;
			client.Config.LogUserName = true;
			client.Config.LogPassword = true;
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
		protected async Task<AsyncFtpClient> GetAsyncClient() {
			var client = new AsyncFtpClient("localhost", new NetworkCredential(_fixture.GetUsername(), _fixture.GetPassword()));
			if (_stream == UseStream.GnuTlsStream) {
				client.Config.CustomStream = typeof(FluentFTP.GnuTLS.GnuTlsStream);
				client.Config.CustomStreamConfig = new FluentFTP.GnuTLS.GnuConfig();
			}
			client.Config.EncryptionMode = FtpEncryptionMode.Auto;
			client.Config.ValidateAnyCertificate = true;
			client.Config.LogHost = true;
			client.Config.LogUserName = true;
			client.Config.LogPassword = true;
			return client;
		}

		/// <summary>
		/// Creates & Connects a new FTP client capable of connecting to this dockerized FTP server.
		/// </summary>
		protected async Task<AsyncFtpClient> GetConnectedAsyncClient() {
			var client = await GetAsyncClient();
			await client.AutoConnect();
			return client;
		}

	}
}
