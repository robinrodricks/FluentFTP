using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FluentFTP.Xunit.Docker.Containers {
	internal class CrushFtpContainer : DockerFtpContainer {

		public CrushFtpContainer() {
			Type = FtpServer.CrushFTP;
			ServerType = "crushftp";
			DockerImage = "markusmcnugen/crushftp";
			DockerGithub = "https://github.com/MarkusMcNugen/docker-CrushFTP";
			//RunCommand = "docker run -p 21:21 -p 443:443 -p 2000-2100:2000-2100 -p 2222:2222 -p 8080:8080 -p 9090:9090 crushftp:fluentftp";
			//FtpUser = "crushadmin";
			//FtpPass = "crushadmin";
		}

		/// <summary>
		/// For help creating this section see https://github.com/testcontainers/testcontainers-dotnet#supported-commands
		/// </summary>
		public override ITestcontainersBuilder<TestcontainersContainer> Configure(ITestcontainersBuilder<TestcontainersContainer> builder) {

			builder = builder
				.WithPortBinding(443)
				.WithPortBinding(2222)
				.WithPortBinding(8080)
				.WithPortBinding(9090);

			builder = ExposePortRange(builder, 2000, 2100);
			//todo AutoConnect failing: FtpInvalidCertificateException : FTPS security could not be established on the server. The certificate was not accepted.
			//todo volume
			builder = builder
				.WithEnvironment("CRUSH_ADMIN_USER", DockerFtpConfig.FtpUser)
				.WithEnvironment("CRUSH_ADMIN_PASSWORD", DockerFtpConfig.FtpPass)
				.WithEnvironment("CRUSH_ADMIN_PORT", "8080");

			return builder;
		}
	}
}
