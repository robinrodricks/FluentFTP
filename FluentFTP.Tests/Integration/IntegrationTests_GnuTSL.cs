using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentFTP.Tests.Integration.System;

namespace FluentFTP.Tests.Integration {

	[Collection("DockerTests")]
	public class IntegrationTests_GnuTLS {

		private static bool UseSsl = true;

		// GnuTLS

		// These can do both FTP and FTPS
		[Fact]
		public async Task ProFtpd() {
			await IntegrationTestRunner.Run(FtpServer.ProFTPD, UseStream.GnuTlsStream);
		}
		[Fact]
		public async Task ProFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.ProFTPD, UseStream.GnuTlsStream, UseSsl);
		}
		[Fact]
		public async Task PureFtpd() {
			await IntegrationTestRunner.Run(FtpServer.PureFTPd, UseStream.GnuTlsStream);
		}
		[Fact]
		public async Task PureFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.PureFTPd, UseStream.GnuTlsStream, UseSsl);
		}
		[Fact]
		public async Task VsFtpd() {
			await IntegrationTestRunner.Run(FtpServer.VsFTPd, UseStream.GnuTlsStream);
		}
		[Fact]
		public async Task VsFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.VsFTPd, UseStream.GnuTlsStream, UseSsl);
		}

		// These can only do FTPS

		// Works, but needs some TLC, runs very slowly - each GnuTLS BYE hangs for while,
		// does not happen on a real FileZilla Server
		//[Fact]
		//public async Task FileZillaSsl() {
		//	await IntegrationTestRunner.Run(FtpServer.FileZilla, UseStream.GnuTlsStream);
		//}
		// Works, but needs some TLC. Image does not always start reliably, hangs
		//[Fact]
		//public async Task GlftpdSsl() {
		//	await IntegrationTestRunner.Run(FtpServer.glFTPd, UseStream.GnuTlsStream);
		//}

		// These can only do FTP
		[Fact]
		public async Task Apache() {
			await IntegrationTestRunner.Run(FtpServer.Apache, UseStream.GnuTlsStream);
		}
		[Fact]
		public async Task Bftpd() {
			await IntegrationTestRunner.Run(FtpServer.BFTPd, UseStream.GnuTlsStream);
		}

		// Still need SSL variants of these
		[Fact]
		public async Task PyFtpdLib() {
			await IntegrationTestRunner.Run(FtpServer.PyFtpdLib, UseStream.GnuTlsStream);
		}

	}
}
