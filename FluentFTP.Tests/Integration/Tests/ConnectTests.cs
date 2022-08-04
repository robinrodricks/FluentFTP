using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentFTP.Xunit.Docker;
using FluentFTP.Xunit.Attributes;
using FluentFTP.Tests.Integration.System;

namespace FluentFTP.Tests.Integration.Tests {

	internal class ConnectTests : IntegrationTestSuite {

		public ConnectTests(DockerFtpServer fixture) : base(fixture) { }


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
			using var ftpClient = GetClient();
			await ftpClient.ConnectAsync();
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
			using var ftpClient = GetClient();
			var profile = await ftpClient.AutoConnectAsync();
			Assert.NotNull(profile);
		}


		public void AutoDetect() {
			using var ftpClient = GetClient();
			var profiles = ftpClient.AutoDetect();
			Assert.NotEmpty(profiles);
		}


		public async Task AutoDetectAsync() {
			using var ftpClient = GetClient();
			var profiles = await ftpClient.AutoDetectAsync(false);
			Assert.NotEmpty(profiles);
		}

	}
}