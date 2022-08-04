using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentFTP.Xunit.Docker;
using FluentFTP.Xunit.Attributes;

namespace FluentFTP.Tests.Integration.System {

	public class IntegrationTestSuite {

		protected readonly DockerFtpServer _fixture;

		public IntegrationTestSuite(DockerFtpServer fixture) {
			_fixture = fixture;
		}

		/// <summary>
		/// Main entrypoint executed for all types of FTP servers.
		/// </summary>
		public virtual void RunAllTests() {
		}

		/// <summary>
		/// Main entrypoint executed for all types of FTP servers.
		/// </summary>
		public async virtual Task RunAllTestsAsync() {
		}

		/// <summary>
		/// Creates a new FTP client capable of connecting to this dockerized FTP server.
		/// </summary>
		protected FtpClient GetClient() {
			var client = new FtpClient("localhost", _fixture.GetUsername(), _fixture.GetPassword());
			return client;
		}

		/// <summary>
		/// Creates & Connects a new FTP client capable of connecting to this dockerized FTP server.
		/// </summary>
		protected FtpClient GetConnectedClient() {
			var client = GetClient();
			client.AutoConnect();
			return client;
		}

	}
}
