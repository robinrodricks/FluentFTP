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
			DockerImage = "jlesage/filezilla";
			DockerGithub = "https://github.com/jlesage/docker-filezilla";
			//RunCommand = "docker run -d --name=filezilla -p 5800:5800 -v /docker/appdata/filezilla:/config:rw -v $HOME:/storage:rw filezilla:fluentftp";
			//FtpUser = "filezilla";
			//FtpPass = "filezilla";
		}
		public override void Configure(ITestcontainersBuilder<TestcontainersContainer> builder) {

			builder
				.WithPortBinding(5800)
				.WithBindMount("/docker/appdata/filezilla", "/config:rw")
				.WithBindMount("$HOME", "/storage:rw");

		}
	}
}
