namespace FluentFTP.Xunit.Docker.Containers {
	internal class FileZillaContainer : DockerFtpContainer {

		//without SSL:
		// not possible
		//with SSL:
		// RunCommand = "docker run --rm -it -p 21:21 -p 21100-21199:21100-21199 filezilla:fluentftp";
		public FileZillaContainer() {
			ServerType = FtpServer.FileZilla;
			ServerName = "filezilla";
			DockerImage = "filezilla:fluentftp";
		}
	}
}
