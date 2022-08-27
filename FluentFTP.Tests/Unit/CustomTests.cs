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
			return new FtpClient("", 21, "", "");
		}
		private async Task<AsyncFtpClient> NewTestAsyncClient() {
			return new AsyncFtpClient("", 21, "", "");
		}

		[Fact]
		public void AutoConnect() {

			var client = NewTestClient();
			client.AutoConnect();
		}

		[Fact]
		public async Task AutoConnectAsync() {

			var client = await NewTestAsyncClient();
			await client.AutoConnectAsync();
		}


	}
}
