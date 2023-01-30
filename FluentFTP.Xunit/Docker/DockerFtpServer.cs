using DotNet.Testcontainers.Containers;
using FluentFTP.Xunit.Attributes;

namespace FluentFTP.Xunit.Docker {
	public class DockerFtpServer : IDisposable {
		internal DockerFtpContainer _server;
		internal TestcontainersContainer? _container;
		internal bool _useSsl;
		internal string _useStream;

		public DockerFtpServer(FtpServer serverType, string useStream, bool useSsl) {

			// find the server
			_server = DockerFtpContainerIndex.Index.FirstOrDefault(s => s.ServerType == serverType)!;
			if (_server == null) {
				throw new ArgumentException("Server type '" + serverType + "' cannot be found! You can contribute support for this server! See https://github.com/robinrodricks/FluentFTP/wiki/Automated-Testing.");
			}

			_useSsl = useSsl;
			_useStream = useStream;

			// build and start the container image
			StartContainer();
		}

		~DockerFtpServer() {
			Dispose(false);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool _) {
			_container?.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
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

		private void StartContainer() {
			if (_server != null) {
				try {
					// dispose existing container if any
					_container?.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();

					// build the container image
					_container = _server.Build(_useStream, _useSsl);

					// start the container
					_container.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
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