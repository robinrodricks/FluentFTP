using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentFTP.Tests.Integration.System;

namespace FluentFTP.Tests.Integration {
	public class IntegrationTests {

		[Fact]
		public async Task CrushFtp() {
			await IntegrationTestRunner.Run(FtpServer.CrushFTP);
		}
		[Fact]
		public async Task FileZilla() {
			await IntegrationTestRunner.Run(FtpServer.FileZilla);
		}
		[Fact]
		public async Task GlFtpd() {
			await IntegrationTestRunner.Run(FtpServer.glFTPd);
		}
		[Fact]
		public async Task ProFtpd() {
			await IntegrationTestRunner.Run(FtpServer.ProFTPD);
		}
		[Fact]
		public async Task PureFtpd() {
			await IntegrationTestRunner.Run(FtpServer.PureFTPd);
		}
		[Fact]
		public async Task PyFtpdLib() {
			await IntegrationTestRunner.Run(FtpServer.PyFtpdLib);
		}
		[Fact]
		public async Task VsFtpd() {
			await IntegrationTestRunner.Run(FtpServer.VsFTPd);
		}

	}
}
