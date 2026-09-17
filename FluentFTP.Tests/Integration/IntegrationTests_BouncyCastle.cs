using System.Threading.Tasks;
using Xunit;
using FluentFTP.Tests.Integration.System;

namespace FluentFTP.Tests.Integration {

	[Collection("DockerTests")]
	public class IntegrationTests_BouncyCastle {

		private const bool UseSsl = true;

		// vsftpd(ssl) and proftpd(ssl) REFUSE data connections that do not resume the control session, so they
		// are the IDEAL candidates for testing the BouncyCastle stream. All other servers should of course work
		// but do not prove the BouncyCastle streams special capability of resuming the control session on data connections.

		// These can do both FTP and FTPS
		[Fact]
		public async Task ProFtpd() {
			await IntegrationTestRunner.Run(FtpServer.ProFTPD, UseStream.BouncyCastleStream);
		}
		[Fact]
		public async Task ProFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.ProFTPD, UseStream.BouncyCastleStream, UseSsl);
		}
		[Fact]
		public async Task PureFtpd() {
			await IntegrationTestRunner.Run(FtpServer.PureFTPd, UseStream.BouncyCastleStream);
		}
		[Fact]
		public async Task PureFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.PureFTPd, UseStream.BouncyCastleStream, UseSsl);
		}
		[Fact]
		public async Task VsFtpd() {
			await IntegrationTestRunner.Run(FtpServer.VsFTPd, UseStream.BouncyCastleStream);
		}
		[Fact]
		public async Task VsFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.VsFTPd, UseStream.BouncyCastleStream, UseSsl);
		}

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
