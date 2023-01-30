using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentFTP;

namespace FluentFTP.Xunit.Docker {
	internal class DockerFtpContainer {

		public FtpServer ServerType;
		public string ServerName;
		public string DockerImage;
		public string DockerImageOriginal;
		public string DockerGithub;
		public string FixedUsername;
		public string FixedPassword;

		public virtual ITestcontainersBuilder<TestcontainersContainer> Configure(ITestcontainersBuilder<TestcontainersContainer> builder) {
			return builder;
		}

		public virtual TestcontainersContainer Build(string useStream, bool useSsl = false) {

			var builder = new TestcontainersBuilder<TestcontainersContainer>()
				.WithImage(DockerImage)
				.WithName(ServerName + "_" + useStream)
				.WithPortBinding(21);

			builder = this.Configure(builder);

			builder = builder.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(21));

			if (useSsl) {
				builder = builder.WithEnvironment("USE_SSL", "YES");
			}

			var container = builder.Build();

			return container;
		}

		protected ITestcontainersBuilder<TestcontainersContainer> ExposePortRange(ITestcontainersBuilder<TestcontainersContainer> builder, int startPort, int endPort) {

			for (var port = startPort; port <= endPort; port++) {
				builder = builder.WithExposedPort(port);
				builder = builder.WithPortBinding(port);
			}

			return builder;
		}

	}
}
