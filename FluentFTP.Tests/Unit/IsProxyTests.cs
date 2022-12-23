using FluentFTP.Client.BaseClient;
using FluentFTP.Proxy.AsyncProxy;
using FluentFTP.Proxy.SyncProxy;
using Xunit;

namespace FluentFTP.Tests.Unit {
	public class IsProxyTests {
		[MemberData(nameof(ProxyFtpClients))]
		[Theory]
		public void Proxy_clients_are_proxies(BaseFtpClient ftpClient) {
			// Act
			var isProxy = ftpClient.IsProxy();

			// Assert
			Assert.True(isProxy);
		}

		public static TheoryData<BaseFtpClient> ProxyFtpClients => new() {
			new AsyncFtpClientSocks4aProxy(new FtpProxyProfile()),
			new FtpClientSocks4aProxy(new FtpProxyProfile())
		};

		[MemberData(nameof(FtpClients))]
		[Theory]
		public void Clients_are_not_proxies(BaseFtpClient ftpClient) {
			// Act
			var isProxy = ftpClient.IsProxy();

			// Assert
			Assert.False(isProxy);
		}

		public static TheoryData<BaseFtpClient> FtpClients => new() {
			new AsyncFtpClient(),
			new FtpClient()
		};
	}
}
