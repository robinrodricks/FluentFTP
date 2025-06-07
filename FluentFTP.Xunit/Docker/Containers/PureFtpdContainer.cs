namespace FluentFTP.Xunit.Docker.Containers {
	internal class PureFtpdContainer : DockerFtpContainer {

		//without SSL:
		// RunCommand = "docker run --rm -it -p 21:21 -p 21100-21199:21100-21199 pureftpd:fluentftp";
		//with SSL:
		// RunCommand = "docker run --rm -it -p 21:21 -p 21100-21199:21100-21199 -e USE_SSL=YES pureftpd:fluentftp";
		public PureFtpdContainer() {
			ServerType = FtpServer.PureFTPd;
			ServerName = "pureftpd";
			DockerImage = "pureftpd:fluentftp";
			CreateParms = new List<string> {
					"SYS_NICE",
					"DAC_READ_SEARCH"
			};
		}
	}
}