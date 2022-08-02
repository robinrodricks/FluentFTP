using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FluentFTP.Xunit.Docker.Containers {
	internal class FileZillaContainer : DockerFtpContainer {
		public FileZillaContainer() {
			Type = FtpServer.FileZilla;
			ServerType = "filezilla";
			//todo Find server. This seems to be only the client (accessable via webUI).
			DockerImage = "jlesage/filezilla";
			DockerGithub = "https://github.com/jlesage/docker-filezilla";
			//RunCommand = "docker run -d --name=filezilla -p 5800:5800 -v /docker/appdata/filezilla:/config:rw -v $HOME:/storage:rw filezilla:fluentftp";
			FixedUsername = "filezilla";
			FixedPassword = "filezilla";
		}

		/// <summary>
		/// For help creating this section see https://github.com/testcontainers/testcontainers-dotnet#supported-commands
		/// </summary>
		public override ITestcontainersBuilder<TestcontainersContainer> Configure(ITestcontainersBuilder<TestcontainersContainer> builder) {

			builder = builder
				.WithPortBinding(5800)
				.WithBindMount("/docker/appdata/filezilla", "/config:rw")
				.WithBindMount("$HOME", "/storage:rw");

			return builder;
		}
	}
}
