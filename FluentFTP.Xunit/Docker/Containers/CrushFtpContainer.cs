using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FluentFTP.Xunit.Docker.Containers {
	internal class CrushFtpContainer : DockerFtpContainer {

		public CrushFtpContainer() {
			Type = FtpServer.CrushFTP;
			ServerType = "crushftp";
			DockerImage = "markusmcnugen/crushftp";
			//RunCommand = "docker run -p 21:21 -p 443:443 -p 2000-2100:2000-2100 -p 2222:2222 -p 8080:8080 -p 9090:9090 crushftp:fluentftp";
			//FtpUser = "crushadmin";
			//FtpPass = "crushadmin";
		}
		public override void Configure(ITestcontainersBuilder<TestcontainersContainer> builder) {
		}
	}
}
