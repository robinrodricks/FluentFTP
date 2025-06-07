namespace FluentFTP.Xunit.Docker.Containers {
	internal class ApacheContainer : DockerFtpContainer {

		//without SSL:
		// RunCommand = "docker run --rm -it -p 21:21 -p 21100-21199:21100-21199 apache:fluentftp";
		//with SSL:
		// not possible
		public ApacheContainer() {
			ServerType = FtpServer.Apache;
			ServerName = "apache";
			DockerImage = "apache:fluentftp";
		}
	}
}
