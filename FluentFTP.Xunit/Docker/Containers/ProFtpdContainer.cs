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
			//without SSL:
			// RunCommand = "docker run -d --net host proftpd:fluentftp";
			//with SSL:
			// RunCommand = "docker run -d --net host -e USE_SSL=YES proftpd:fluentftp";
		}

		/// <summary>
		/// For help creating this section see https://github.com/testcontainers/testcontainers-dotnet#supported-commands
		/// </summary>
		public override ITestcontainersBuilder<TestcontainersContainer> Configure(ITestcontainersBuilder<TestcontainersContainer> builder) {

			builder = builder.WithPortBinding(20);

			builder = ExposePortRange(builder, 21100, 21199);

			return builder;
		}
	}
}
