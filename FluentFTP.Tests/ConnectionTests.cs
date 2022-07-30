using FluentFTP.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Xunit;
using Xunit.Sdk;

namespace FluentFTP.Tests {
	public class ConnectionTests {

		[Fact]
		public void ConnectTimeout() {

			FtpClient client = new FtpClient("test.github.com", 21, "wrong", "password");
			client.DataConnectionType = FtpDataConnectionType.PASVEX;
			client.ConnectTimeout = 2500;
			var start = DateTime.Now;
			try {
				client.Connect();
			}
			catch (TimeoutException) {
				if ((DateTime.Now - start).TotalMilliseconds > 3000) {
					new XunitException("ConnectTimeout is being ignored with Connect() method!");
				}
				else {
					Assert.True(true);
				}
			}
		}

		[Fact]
		public async void ConnectTimeoutAsync() {

			FtpClient client = new FtpClient("test.github.com", 21, "wrong", "password");
			client.DataConnectionType = FtpDataConnectionType.PASVEX;
			client.ConnectTimeout = 2500;
			var start = DateTime.Now;
			try {
				await client.ConnectAsync();
			}
			catch (TimeoutException) {
				if ((DateTime.Now - start).TotalMilliseconds > 3000) {
					new XunitException("ConnectTimeout is being ignored with Connect() method!");
				}
				else {
					Assert.True(true);
				}
			}
		}

	}
}