namespace FluentFTP.Xunit.Docker.Containers {
	internal class ProFtpdContainer : DockerFtpContainer {

		//without SSL:
		// RunCommand = "docker run -d --net host proftpd:fluentftp";
		//with SSL:
		// RunCommand = "docker run -d --net host -e USE_SSL=YES proftpd:fluentftp";
		public ProFtpdContainer() {
			ServerType = FtpServer.ProFTPD;
			ServerName = "proftpd";
			DockerImage = "proftpd:fluentftp";
		}
	}
}
