using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FluentFTP.Xunit.Docker.Containers {
	internal class ApacheContainer : DockerFtpContainer {

		public ApacheContainer() {
			ServerType = FtpServer.Apache;
			ServerName = "apache";
			DockerImage = "apache:fluentftp";
			//without SSL:
			// RunCommand = "docker run --rm -it -p 21:21 -p 21100-21199:21100-21199 apache:fluentftp";
			//with SSL:
			// not possible
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
