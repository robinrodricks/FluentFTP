using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentFTP.Xunit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP.Xunit.Docker {
	public class DockerFtpServerFixture : IDisposable {
		internal TestcontainersContainer _container;

		public DockerFtpServerFixture() {

			// Get the current server type to use
			var serverType = GetServerType();

			// build and start the container image
			StartContainer(serverType);
		}

		public void Dispose() {
			_container?.DisposeAsync();
		}

		private static string GetServerType() {

			// read the given static config if running locally
			if (DockerFtpConfig.ServerType != null) {
				return DockerFtpConfig.ServerType;
			}

			// read server type from env var (from Github Actions)
			var serverType = Environment.GetEnvironmentVariable("FluentFTP__Tests__Integration__FtpServerKey");
			if (string.IsNullOrEmpty(serverType)) {
				return "pureftpd";
			}
			else {
				return serverType;
			}
		}


		private async void StartContainer(string key) {

			// find the server
			var server = DockerFtpContainerIndex.Index.FirstOrDefault(s => s.ServerType.Equals(key, StringComparison.OrdinalIgnoreCase));
			if (server != null) {
				try {
					// dispose existing container if any
					_container?.DisposeAsync();

					// build the container image
					_container = server.Build();

					// start the container
					_container.StartAsync().Wait();
				}
				catch (TypeInitializationException ex) {

					// Probably because docker is not running on the machine.
					if (DockerFtpConfig.IsCI)
						throw new InvalidOperationException("Unable to setup FTP server for integration test. TypeInitializationException.", ex);

					SkippableState.ShouldSkip = true;
				}
			}
		}

	}
}