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

namespace FluentFTP.Tests.Integration {
	[Collection("DockerCollection")]
	public class IntegrationTestSuite {

		protected readonly DockerFtpServerFixture _fixture;

		public IntegrationTestSuite(DockerFtpServerFixture fixture) {
			_fixture = fixture;
		}

		protected FtpClient GetClient() {
			var client = new FtpClient("localhost", _fixture.GetUsername(), _fixture.GetPassword());
			return client;
		}

		protected FtpClient GetConnectedClient() {
			var client = GetClient();
			client.AutoConnect();
			return client;
		}

	}
}
