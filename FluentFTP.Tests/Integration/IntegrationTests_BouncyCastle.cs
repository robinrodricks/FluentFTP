using System.Threading.Tasks;
using Xunit;
using FluentFTP.Tests.Integration.System;

namespace FluentFTP.Tests.Integration {

	[Collection("DockerTests")]
	public class IntegrationTests_BouncyCastle {

		private const bool UseSsl = true;

		// Bouncy Castle: FTPS with TLS session reuse on the data connections. vsftpd and proftpd
		// refuse data connections that do not resume the control session, so they exercise it.
		// The docker servers use self-signed certificates, so these tests accept any certificate
		// like the other streams do; certificate and host name checks are covered by the unit tests.

		[Fact]
		public async Task ProFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.ProFTPD, UseStream.BouncyCastleStream, UseSsl);
		}
		[Fact]
		public async Task VsFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.VsFTPd, UseStream.BouncyCastleStream, UseSsl);
		}

	}
}
