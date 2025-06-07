using System;
namespace FluentFTP.Xunit.Docker.Containers {
	internal class PyFtpdLibContainer : DockerFtpContainer {

		//RunCommand = "docker run -it --rm -p 21:21 pyftpdlib:fluentftp";
		public PyFtpdLibContainer() {
			ServerType = FtpServer.PyFtpdLib;
			ServerName = "pyftpdlib";
			DockerImage = "pyftpdlib:fluentftp";
			DockerImageOriginal = "akogut/docker-pyftpdlib";
			DockerGithub = "https://github.com/andriykohut/docker-pyftpdlib";
			FixedUsername = "user";
			FixedPassword = "password";
			ExposedPortRangeBegin = 3000;
			ExposedPortRangeEnd = 3010;
		}
	}
}
