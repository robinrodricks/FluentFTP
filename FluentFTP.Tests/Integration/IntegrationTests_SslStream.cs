using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentFTP.Tests.Integration.System;

namespace FluentFTP.Tests.Integration {

	[Collection("DockerTests")]
	public class IntegrationTests_SslStream {

		private static bool UseSsl = true;

		// These can do both FTP and FTPS
		[Fact]
		public async Task ProFtpd() {
			await IntegrationTestRunner.Run(FtpServer.ProFTPD, UseStream.SslStream);
		}
		[Fact]
		public async Task ProFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.ProFTPD, UseStream.SslStream, UseSsl);
		}
		[Fact]
		public async Task PureFtpd() {
			await IntegrationTestRunner.Run(FtpServer.PureFTPd, UseStream.SslStream);
		}
		[Fact]
		public async Task PureFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.PureFTPd, UseStream.SslStream, UseSsl);
		}
		[Fact]
		public async Task VsFtpd() {
			await IntegrationTestRunner.Run(FtpServer.VsFTPd, UseStream.SslStream);
		}
		[Fact]
		public async Task VsFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.VsFTPd, UseStream.SslStream, UseSsl);
		}

		// These can only do FTPS
		[Fact]
		public async Task FileZillaSsl() {
			await IntegrationTestRunner.Run(FtpServer.FileZilla, UseStream.SslStream);
		}
		// Works, but needs some TLC. Image does not always start reliably, hangs
		//[Fact]
		//public async Task GlftpdSsl() {
		//	await IntegrationTestRunner.Run(FtpServer.glFTPd, UseStream.SslStream);
		//}

		// These can only do FTP
		[Fact]
		public async Task Apache() {
			await IntegrationTestRunner.Run(FtpServer.Apache, UseStream.SslStream);
		}
		[Fact]
		public async Task Bftpd() {
			await IntegrationTestRunner.Run(FtpServer.BFTPd, UseStream.SslStream);
		}

		// Still need SSL variants of these
		[Fact]
		public async Task PyFtpdLib() {
			await IntegrationTestRunner.Run(FtpServer.PyFtpdLib, UseStream.SslStream);
		}

	}
}
