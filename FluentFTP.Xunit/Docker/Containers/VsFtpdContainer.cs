namespace FluentFTP.Xunit.Docker.Containers {
	internal class VsFtpdContainer : DockerFtpContainer {

		//without SSL:
		// RunCommand = "docker run --rm -it -p 21:21 -p 21100-21199:21100-21199 vsftpd:fluentftp";
		//with SSL:
		// RunCommand = "docker run --rm -it -p 21:21 -p 21100-21199:21100-21199 -e USE_SSL=YES vsftpd:fluentftp";
		public VsFtpdContainer() {
			ServerType = FtpServer.VsFTPd;
			ServerName = "vsftpd";
			DockerImage = "vsftpd:fluentftp";
		}
	}
}
