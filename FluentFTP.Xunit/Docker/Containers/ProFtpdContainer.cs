using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FluentFTP.Xunit.Docker.Containers {
	internal class ProFtpdContainer : DockerFtpContainer {

		public ProFtpdContainer() {
			ServerType = FtpServer.ProFTPD;
			ServerName = "proftpd";
			DockerImage = "proftpd:fluentftp";
			DockerImageOriginal = "kibatic/proftpd";
			DockerGithub = "https://github.com/kibatic/docker-proftpd";
			//RunCommand = "docker run -d --net host -e FTP_LIST=\"fluentroot:fluentpass\" -e MASQUERADE_ADDRESS=1.2.3.4 proftpd:fluentftp";
		}

		/// <summary>
		/// For help creating this section see https://github.com/testcontainers/testcontainers-dotnet#supported-commands
		/// </summary>
		public override ITestcontainersBuilder<TestcontainersContainer> Configure(ITestcontainersBuilder<TestcontainersContainer> builder) {

			builder = ExposePortRange(builder, 50000, 50010);

			builder = builder
				.WithEnvironment("FTP_LIST", DockerFtpConfig.FtpUser + ":" + DockerFtpConfig.FtpPass)
				.WithEnvironment("PASSIVE_MIN_PORT", "50000")
				.WithEnvironment("PASSIVE_MAX_PORT", "50010")
				.WithEnvironment("MASQUERADE_ADDRESS", "127.0.0.1");

			return builder;
		}
	}
}
