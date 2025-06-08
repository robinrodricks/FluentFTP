using FluentFTP.Xunit.Docker.Containers;

namespace FluentFTP.Xunit.Docker {
	internal static class DockerFtpContainerIndex {

		public static List<DockerFtpContainer> Index = new List<DockerFtpContainer> {
			new ApacheContainer(),
			new BFtpdContainer(),
			new GlFtpdContainer(),
			new ProFtpdContainer(),
			new PureFtpdContainer(),
			new PyFtpdLibContainer(),
			new VsFtpdContainer(),
		};
	}
}
