using FluentFTP.Tests.Integration.Tests;
using FluentFTP.Xunit.Docker;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FluentFTP.Tests.Integration.System {
	internal static class IntegrationTestRunner {

		public static async Task Run(FtpServer serverType, UseStream useStream, bool useSsl = false) {

			// If we are in CI pipeline
			if (DockerFtpConfig.IsCI) {

				// just let the test pass
				return;
			}

			// spin up a new docker
			using var server = new DockerFtpServer(serverType, useStream.ToString(), useSsl);

			try {
				// TODO: create a better system instead of calling each test suite manually

				// run all types of sync tests
				new ConnectTests(server, useStream).RunAllTests();
				new FileTransferTests(server, useStream).RunAllTests();
				new ListingTests(server, useStream).RunAllTests();

				// run all types of async tests
				await new ConnectTests(server, useStream).RunAllTestsAsync();
				await new FileTransferTests(server, useStream).RunAllTestsAsync();
				await new ListingTests(server, useStream).RunAllTestsAsync();

			}
			catch (Exception ex) {
				Assert.True(false, $"Integration test failed : " + ex.ToString());
			}

		}

	}
}
