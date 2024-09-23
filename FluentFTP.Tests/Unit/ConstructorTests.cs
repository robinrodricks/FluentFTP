using System;
using System.Net;
using Xunit;

namespace FluentFTP.Tests.Unit {
	public class ConstructorTests {

		const string defaultHost = "127.0.0.1";

		[Fact]
		public void FtpClient_NullCredentials_Throws() {
			Assert.Throws<ArgumentNullException>(() => new FtpClient(defaultHost, null));
		}

		[Fact]
		public void AsyncFtpClient_NullCredentials_Throws() {
			Assert.Throws<ArgumentNullException>(() => new AsyncFtpClient(defaultHost, null));
		}

		[Fact]
		public void FtpClient_EmptyUserNameCredential_Throws() {
			Assert.Throws<ArgumentException>(() => new FtpClient(defaultHost, new NetworkCredential("", "")));
		}

		[Fact]
		public void FtpClient_NullUserName_Throws() {
			Assert.Throws<ArgumentNullException>(() => new FtpClient(defaultHost, null, ""));
		}

		[Fact]
		public void FtpClient_NullUserPassword_Throws() {
			Assert.Throws<ArgumentNullException>(() => new FtpClient(defaultHost, "test", null));
		}

		[Fact]
		public void FtpClient_EmptyUserName_Throws() {
			Assert.Throws<ArgumentException>(() => new FtpClient(defaultHost, "", ""));
		}

		[Fact]
		public void AsyncFtpClient_EmptyUserNameCredential_Throws() {
			Assert.Throws<ArgumentException>(() => new AsyncFtpClient(defaultHost, new NetworkCredential("", "")));
		}

		[Fact]
		public void AsyncFtpClient_EmptyUserName_Throws() {
			Assert.Throws<ArgumentException>(() => new AsyncFtpClient(defaultHost, "", ""));
		}

		[Fact]
		public void AsyncFtpClient_NullUserName_Throws() {
			Assert.Throws<ArgumentNullException>(() => new AsyncFtpClient(defaultHost, null, ""));
		}

		[Fact]
		public void AsyncFtpClient_NullUserPassword_Throws() {
			Assert.Throws<ArgumentNullException>(() => new AsyncFtpClient(defaultHost, "test", null));
		}


	}
}
