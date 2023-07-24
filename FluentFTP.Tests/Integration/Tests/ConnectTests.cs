using System;
using System.Threading.Tasks;
using Xunit;

using FluentFTP.Xunit.Docker;
using FluentFTP.Tests.Integration.System;
using FluentFTP.Model.Functions;

namespace FluentFTP.Tests.Integration.Tests {

	internal class ConnectTests : IntegrationTestSuite {

		public ConnectTests(DockerFtpServer fixture, UseStream stream) : base(fixture, stream) { }


		/// <summary>
		/// Main entrypoint executed for all types of FTP servers.
		/// </summary>
		public override void RunAllTests() {
			Connect();
			AutoConnect();
			AutoDetect();
		}

		/// <summary>
		/// Main entrypoint executed for all types of FTP servers.
		/// </summary>
		public async override Task RunAllTestsAsync() {
			await ConnectAsync();
			await AutoConnectAsync();
			await AutoDetectAsync();
		}


		public async Task ConnectAsync() {
			using var ftpClient = await GetAsyncClient();
			await ftpClient.Connect();
			// Connect without error => pass
			Assert.True(true);
		}


		public void Connect() {
			using var ftpClient = GetClient();
			ftpClient.Connect();
			// Connect without error => pass
			Assert.True(true);
		}


		public void AutoConnect() {
			using var ftpClient = GetClient();
			var profile = ftpClient.AutoConnect();
			Assert.NotNull(profile);
		}


		public async Task AutoConnectAsync() {
			using var ftpClient = await GetAsyncClient();
			var profile = await ftpClient.AutoConnect();
			Assert.NotNull(profile);
		}


		public void AutoDetect() {
			using var ftpClient = GetClient();
			var profiles = ftpClient.AutoDetect(new FtpAutoDetectConfig());
			Assert.NotEmpty(profiles);
		}


		public async Task AutoDetectAsync() {
			using var ftpClient = await GetAsyncClient();
			var profiles = await ftpClient.AutoDetect(new FtpAutoDetectConfig());
			Assert.NotEmpty(profiles);
		}

	}
}