using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FluentFTP.Xunit.Docker.Containers {
	internal class GlFtpdContainer : DockerFtpContainer {

		public GlFtpdContainer() {
			Type = FtpServer.glFTPd;
			ServerType = "glftpd";
			DockerImage = "jonarin/glftpd";
			DockerGithub = "https://github.com/jonathanbower/docker-glftpd";
			//RunCommand = "docker run --name=glFTPd --net=host -e GL_PORT=21 -e GL_RESET_ARGS=<arguments> glftpd:fluentftp";
			//FtpUser = "glftpd";
			//FtpPass = "glftpd";
		}
		public override void Configure(ITestcontainersBuilder<TestcontainersContainer> builder) {
		}
	}
}
