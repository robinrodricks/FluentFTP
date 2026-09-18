using FluentFTP.Tests.Integration.System;

namespace FluentFTP.Tests.Integration {

	// vsftpd(ssl) and proftpd(ssl) REFUSE data connections that do not resume the control session, so they
	// are the IDEAL candidates for testing the BouncyCastle stream. All other servers should of course work
	// but do not prove the BouncyCastle streams special capability of resuming the control session on data connections.
	public class IntegrationTests_BouncyCastle : IntegrationTestsBase {

		protected override UseStream Stream => UseStream.BouncyCastleStream;

		// These can only do FTPS

		// Works, but needs some TLC, runs very slowly - each BouncyCastle BYE hangs for while,
		// does not happen on a real FileZilla Server
		//[Fact]
		//public async Task FileZillaSsl() {
		//	await IntegrationTestRunner.Run(FtpServer.FileZilla, UseStream.BouncyCastleStream);
		//}
		// Works, but needs some TLC. Image does not always start reliably, hangs
		//[Fact]
		//public async Task GlftpdSsl() {
		//	await IntegrationTestRunner.Run(FtpServer.glFTPd, UseStream.BouncyCastleStream);
		//}

		// These can only do FTP
		[Fact]
		public async Task Apache() {
			await IntegrationTestRunner.Run(FtpServer.Apache, UseStream.BouncyCastleStream);
		}
		[Fact]
		public async Task Bftpd() {
			await IntegrationTestRunner.Run(FtpServer.BFTPd, UseStream.BouncyCastleStream);
		}

		// Still need SSL variants of these
		[Fact]
		public async Task PyFtpdLib() {
			await IntegrationTestRunner.Run(FtpServer.PyFtpdLib, UseStream.BouncyCastleStream);
		}

	}
}
