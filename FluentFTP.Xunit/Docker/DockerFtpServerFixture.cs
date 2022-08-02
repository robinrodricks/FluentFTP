using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentFTP.Xunit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP.Xunit.Docker {
	public class DockerFtpServerFixture : IDisposable {
		internal DockerFtpContainer _server;
		internal TestcontainersContainer _container;

		public DockerFtpServerFixture() {

			// Get the current server type to use
			var serverType = GetServerType();

			// find the server
			_server = DockerFtpContainerIndex.Index.FirstOrDefault(s => s.ServerType.Equals(serverType, StringComparison.OrdinalIgnoreCase));
			
			// build and start the container image
			StartContainer();
		}

		public void Dispose() {
			_container?.DisposeAsync();
		}

		public string GetUsername() {
			if (_server != null && _server.FixedUsername != null) {
				return _server.FixedUsername;
			}
			return DockerFtpConfig.FtpUser;
		}

		public string GetPassword() {
			if (_server != null && _server.FixedPassword != null) {
				return _server.FixedPassword;
			}
			return DockerFtpConfig.FtpPass;
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


		private async void StartContainer() {
			if (_server != null) {
				try {
					// dispose existing container if any
					_container?.DisposeAsync();

					// build the container image
					_container = _server.Build();

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