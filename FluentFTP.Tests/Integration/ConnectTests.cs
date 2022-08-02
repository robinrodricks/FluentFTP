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

namespace FluentFTP.Tests.Integration {

	/// <summary>
	/// All tests must use [SkippableFact] or [SkippableTheory] to allow "all tests" to run successfully on a developer machine without Docker.
	/// </summary>
	public class ConnectTests : IntegrationTestSuite {

		public ConnectTests(DockerFtpServerFixture fixture) : base(fixture) { }


		[SkippableFact]
		public async Task ConnectAsync() {
			using var ftpClient = GetClient();
			await ftpClient.ConnectAsync();
			// Connect without error => pass
			Assert.True(true);
		}

		[SkippableFact]
		public void Connect() {
			using var ftpClient = GetClient();
			ftpClient.Connect();
			// Connect without error => pass
			Assert.True(true);
		}

		[SkippableFact]
		public void AutoConnect() {
			using var ftpClient = GetClient();
			var profile = ftpClient.AutoConnect();
			Assert.NotNull(profile);
		}

		[SkippableFact]
		public async Task AutoConnectAsync() {
			using var ftpClient = GetClient();
			var profile = await ftpClient.AutoConnectAsync();
			Assert.NotNull(profile);
		}

		[SkippableFact]
		public void AutoDetect() {
			using var ftpClient = GetClient();
			var profiles = ftpClient.AutoDetect();
			Assert.NotEmpty(profiles);
		}

		[SkippableFact]
		public async Task AutoDetectAsync() {
			using var ftpClient = GetClient();
			var profiles = await ftpClient.AutoDetectAsync(false);
			Assert.NotEmpty(profiles);
		}

	}
}