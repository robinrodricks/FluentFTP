namespace FluentFTP.Xunit.Docker.Containers {
	internal class BFtpdContainer : DockerFtpContainer {

		//without SSL:
		// RunCommand = "docker run --rm -it -p 21:21 -p 21100-21199:21100-21199 bftpd:fluentftp";
		//with SSL:
		// not possible
		public BFtpdContainer() {
			ServerType = FtpServer.BFTPd;
			ServerName = "bftpd";
			DockerImage = "bftpd:fluentftp";
		}
	}
}
