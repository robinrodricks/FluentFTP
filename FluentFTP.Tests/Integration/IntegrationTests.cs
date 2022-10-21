using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentFTP.Tests.Integration.System;

namespace FluentFTP.Tests.Integration {
	public class IntegrationTests {

		private static bool UseSsl = true;

		// These can do both FTP and FTPS
		[Fact]
		public async Task VsFtpd() {
			await IntegrationTestRunner.Run(FtpServer.VsFTPd);
		}
		[Fact]
		public async Task VsFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.VsFTPd, UseSsl);
		}
		[Fact]
		public async Task ProFtpd() {
			await IntegrationTestRunner.Run(FtpServer.ProFTPD);
		}
		[Fact]
		public async Task ProFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.ProFTPD, UseSsl);
		}

		// These can only do FTPS
		[Fact]
		public async Task Glftpd() {
			await IntegrationTestRunner.Run(FtpServer.glFTPd);
		}

		[Fact]
		public async Task FileZilla() {
			await IntegrationTestRunner.Run(FtpServer.FileZilla);
		}

		// These can only do FTP
		[Fact]
		public async Task Bftpd() {
			await IntegrationTestRunner.Run(FtpServer.BFTPd);
		}

		// Still need SSL variants of these
		[Fact]
		public async Task PureFtpd() {
			await IntegrationTestRunner.Run(FtpServer.PureFTPd);
		}
		[Fact]
		public async Task PyFtpdLib() {
			await IntegrationTestRunner.Run(FtpServer.PyFtpdLib);
		}

	}
}
