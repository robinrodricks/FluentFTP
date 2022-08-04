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
		public void CrushFtp() {
			IntegrationTestRunner.Run(FtpServer.CrushFTP);
		}
		[Fact]
		public void FileZilla() {
			IntegrationTestRunner.Run(FtpServer.FileZilla);
		}
		[Fact]
		public void GlFtpd() {
			IntegrationTestRunner.Run(FtpServer.glFTPd);
		}
		[Fact]
		public void ProFtpd() {
			IntegrationTestRunner.Run(FtpServer.ProFTPD);
		}
		[Fact]
		public void PureFtpd() {
			IntegrationTestRunner.Run(FtpServer.PureFTPd);
		}
		[Fact]
		public void PyFtpdLib() {
			IntegrationTestRunner.Run(FtpServer.PyFtpdLib);
		}
		[Fact]
		public void VsFtpd() {
			IntegrationTestRunner.Run(FtpServer.VsFTPd);
		}

	}
}
