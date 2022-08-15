using FluentFTP.Tests.Integration.Tests;
using FluentFTP.Xunit.Docker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FluentFTP.Tests.Integration.System {
	internal static class IntegrationTestRunner {

		public static async Task Run(FtpServer serverType) {

			// If we are in CI pipeline
			if (DockerFtpConfig.IsCI) {
				
				// just let the test pass
				return;
			}

			// spin up a new docker
			using var server = new DockerFtpServer(serverType);

			try {

				// run all types of sync tests
				// TODO: create a better system instead of calling each test suite manually
				new ConnectTests(server).RunAllTests();
				new FileTransferTests(server).RunAllTests();
				new ListingTests(server).RunAllTests();

				// run all types of async tests
				// TODO: create a better system instead of calling each test suite manually
				await new ConnectTests(server).RunAllTestsAsync();
				await new FileTransferTests(server).RunAllTestsAsync();
				await new ListingTests(server).RunAllTestsAsync();

			}
			catch (Exception ex) {
				Assert.True(false, $"Integration test failed : " + ex.ToString());
			}


		}

	}
}
