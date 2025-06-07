namespace FluentFTP.Xunit.Docker.Containers {
	internal class GlFtpdContainer : DockerFtpContainer {

		//without SSL:
		// RunCommand = "docker run --rm -it -p 21:21 -p 21100-21199:21100-21199 glftpd:fluentftp";
		//with SSL:
		// not possible
		public GlFtpdContainer() {
			ServerType = FtpServer.glFTPd;
			ServerName = "glftpd";
			DockerImage = "glftpd:fluentftp";
		}
	}
}
