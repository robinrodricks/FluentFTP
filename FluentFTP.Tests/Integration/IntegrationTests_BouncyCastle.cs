using System.Threading.Tasks;
using Xunit;
using FluentFTP.Tests.Integration.System;

namespace FluentFTP.Tests.Integration {

	[Collection("DockerTests")]
	public class IntegrationTests_BouncyCastle {

		private const bool UseSsl = true;

		// Bouncy Castle: FTPS with TLS session reuse on the data connections, connecting by the
		// DNS name "localhost" so the certificate's identity is validated against the host name.
		// vsftpd and proftpd refuse data connections that do not resume the control session.

		[Fact]
		public async Task ProFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.ProFTPD, UseStream.BouncyCastleStream, UseSsl);
		}
		[Fact]
		public async Task PureFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.PureFTPd, UseStream.BouncyCastleStream, UseSsl);
		}
		[Fact]
		public async Task VsFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.VsFTPd, UseStream.BouncyCastleStream, UseSsl);
		}

	}
}
