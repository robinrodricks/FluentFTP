using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FluentFTP.Xunit.Docker.Containers {
	internal class VsFtpdContainer : DockerFtpContainer {

		public VsFtpdContainer() {
			ServerType = FtpServer.VsFTPd;
			ServerName = "vsftpd";
			DockerImage = "vsftpd:fluentftp";
			//without SSL:
			// RunCommand = "docker run --rm -it -p 21:21 -p 21100-21199:21100-21199 vsftpd:fluentftp";
			//with SSL:
			// RunCommand = "docker run --rm -it -p 21:21 -p 21100-21199:21100-21199 -e USE_SSL=YES vsftpd:fluentftp";
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
