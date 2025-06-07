using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FluentFTP.Xunit.Docker {
	internal class DockerFtpContainer {

		public FtpServer ServerType;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
		public string ServerName;
		public string DockerImage;
		public string DockerImageOriginal;
		public string DockerGithub;
		public string FixedUsername;
		public string FixedPassword;
		public List<int> BindingPorts = new List<int> { 20 };
		public int ExposedPortRangeBegin = 21100;
		public int ExposedPortRangeEnd = 21199;
		public List<string> CreateParms = new List<string>();
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

	public virtual DockerContainer Build(string useStream, bool useSsl = false) {

		var builder = new ContainerBuilder()
			.WithImage(DockerImage)
			.WithName(ServerName + "_" + useStream)
			.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(21))
			.WithPortBinding(21);

		if (useSsl) {
			builder = builder.WithEnvironment("USE_SSL", "YES");
		}

		if (BindingPorts.Count > 0) {
			foreach (var port in BindingPorts) {
				builder = builder.WithPortBinding(port);
			}
		}

		if (ExposedPortRangeBegin != 0) {
			for (var port = ExposedPortRangeBegin; port <= ExposedPortRangeEnd; port++) {
				builder = builder.WithExposedPort(port);
				builder = builder.WithPortBinding(port);
			}
		}

		if (CreateParms.Count > 0) {
			builder = builder.WithCreateParameterModifier(x => {
				x.HostConfig.CapAdd = CreateParms;
			});
		}

		var container = builder.Build();

		return (DockerContainer)container;
	}

}
}
