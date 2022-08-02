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
		public override void Configure(ITestcontainersBuilder<TestcontainersContainer> builder) {

			builder.WithPortBinding(443);
			builder.WithPortBinding(2222);
			builder.WithPortBinding(8080);
			builder.WithPortBinding(9090);

			for (var port = 2000; port <= 2100; port++) {
				builder = builder.WithPortBinding(port);
			}

		}
	}
}
