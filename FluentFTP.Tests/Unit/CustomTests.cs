using FluentFTP.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace FluentFTP.Tests.Unit {
	public class CustomTests {

		// This is a template for adding some user customised tests

		private FtpClient NewTestClient() {
			// Customise the following:
			return new FtpClient("1.2.3.4", new NetworkCredential("user", "pass"));
		}
		private AsyncFtpClient NewTestAsyncClient() {
			// Customise the following:
			return new AsyncFtpClient("1.2.3.4", new NetworkCredential("user", "pass"));
		}

		[Fact]
		public void AutoConnect() {

			var client = NewTestClient();
			// Has the test been customised by the user?
			if (client.Host != "1.2.3.4") {
				client.AutoConnect();
				// Add more operations here
				// ...
				// ...
				client.Disconnect();
			}
		}

		[Fact]
		public async Task AutoConnectAsync() {

			var client = NewTestAsyncClient();
			// Has the test been customised by the user?
			if (client.Host != "1.2.3.4") {
				await client.AutoConnect();
				// Add more operations here
				// ...
				// ...
				await client.Disconnect();
			}
		}

	}
}
