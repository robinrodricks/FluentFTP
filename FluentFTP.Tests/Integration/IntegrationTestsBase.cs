using System.Threading.Tasks;
using Xunit;
using FluentFTP.Tests.Integration.System;

namespace FluentFTP.Tests.Integration {

	// Common set of docker-based server tests, run once per stream implementation by derived classes.
	[Collection("DockerTests")]
	public abstract class IntegrationTestsBase {

		private const bool UseSsl = true;

		protected abstract UseStream Stream { get; }

		// These can do both FTP and FTPS
		[Fact]
		public async Task ProFtpd() {
			await IntegrationTestRunner.Run(FtpServer.ProFTPD, Stream);
		}
		[Fact]
		public async Task ProFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.ProFTPD, Stream, UseSsl);
		}
		[Fact]
		public async Task PureFtpd() {
			await IntegrationTestRunner.Run(FtpServer.PureFTPd, Stream);
		}
		[Fact]
		public async Task PureFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.PureFTPd, Stream, UseSsl);
		}
		[Fact]
		public async Task VsFtpd() {
			await IntegrationTestRunner.Run(FtpServer.VsFTPd, Stream);
		}
		[Fact]
		public async Task VsFtpdSsl() {
			await IntegrationTestRunner.Run(FtpServer.VsFTPd, Stream, UseSsl);
		}

		// These can only do FTP
		[Fact]
		public async Task Apache() {
			await IntegrationTestRunner.Run(FtpServer.Apache, Stream);
		}
		[Fact]
		public async Task Bftpd() {
			await IntegrationTestRunner.Run(FtpServer.BFTPd, Stream);
		}

		// Still need SSL variants of these
		[Fact]
		public async Task PyFtpdLib() {
			await IntegrationTestRunner.Run(FtpServer.PyFtpdLib, Stream);
		}

		// These can only do FTPS

		// Works, but needs some TLC, runs very slowly - each stream BYE hangs for while,
		// does not happen on a real FileZilla Server
		//[Fact]
		//public async Task FileZillaSsl() {
		//	await IntegrationTestRunner.Run(FtpServer.FileZilla, Stream);
		//}
		// Works, but needs some TLC. Image does not always start reliably, hangs
		//[Fact]
		//public async Task GlftpdSsl() {
		//	await IntegrationTestRunner.Run(FtpServer.glFTPd, Stream);
		//}

	}
}
