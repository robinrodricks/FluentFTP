using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FluentFTP.Xunit.Docker.Containers {
	internal class PyFtpdLibContainer : DockerFtpContainer {
		public PyFtpdLibContainer() {
			ServerType = FtpServer.PyFtpdLib;
			ServerName = "pyftpdlib";
			DockerImage = "pyftpdlib:fluentftp";
			DockerImageOriginal = "akogut/docker-pyftpdlib";
			DockerGithub = "https://github.com/andriykohut/docker-pyftpdlib";
			//RunCommand = "docker run -it --rm -p 21:21 pyftpdlib:fluentftp";
			FixedUsername = "user";
			FixedPassword = "password";
		}

		/// <summary>
		/// For help creating this section see https://github.com/testcontainers/testcontainers-dotnet#supported-commands
		/// </summary>
		public override ITestcontainersBuilder<TestcontainersContainer> Configure(ITestcontainersBuilder<TestcontainersContainer> builder) {

			builder = base.ExposePortRange(builder, 3000, 3010);
			return builder;
		}
	}
}
