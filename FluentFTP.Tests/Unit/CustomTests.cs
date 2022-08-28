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

		private FtpClient NewTestClient() {
			return new FtpClient("1.2.3.4", new NetworkCredential("user", "pass"));
		}
		private async Task<AsyncFtpClient> NewTestAsyncClient() {
			return new AsyncFtpClient("1.2.3.4", new NetworkCredential("user", "pass"));
		}

		[Fact]
		public void AutoConnect() {

			var client = NewTestClient();
			client.AutoConnect();
		}

		[Fact]
		public async Task AutoConnectAsync() {

			var client = await NewTestAsyncClient();
			await client.AutoConnect();
		}


	}
}
