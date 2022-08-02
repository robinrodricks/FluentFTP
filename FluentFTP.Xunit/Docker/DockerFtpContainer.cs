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

		public FtpServer Type;
		public string ServerType;
		public string DockerImage;
		public string DockerGithub;
		public string FixedUsername;
		public string FixedPassword;

		public virtual void Configure(ITestcontainersBuilder<TestcontainersContainer> builder) {
		}
		
		public virtual TestcontainersContainer Build() {

			var builder = new TestcontainersBuilder<TestcontainersContainer>()
				.WithImage(DockerImage)
				.WithName(ServerType)
				.WithPortBinding(21);

			this.Configure(builder);

			builder.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(21));

			var container = builder.Build();

			return container;
		}

		protected void ExposePortRange(ITestcontainersBuilder<TestcontainersContainer> builder, int startPort, int endPort) {

			for (var port = startPort; port <= endPort; port++) {
				builder.WithExposedPort(port);
				builder.WithPortBinding(port);
			}
		}

	}
}
