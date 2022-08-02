using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FluentFTP.Xunit.Docker.Containers {
	internal class ProFtpdContainer : DockerFtpContainer {

		public ProFtpdContainer() {
			Type = FtpServer.ProFTPD;
			ServerType = "proftpd";
			DockerImage = "kibatic/proftpd";
			//RunCommand = "docker run -d --net host -e FTP_LIST=\"fluentroot:fluentpass\" -e MASQUERADE_ADDRESS=1.2.3.4 proftpd:fluentftp";
			//FtpUser = "fluentroot";
			//FtpPass = "fluentpass";
		}
		public override void Configure(ITestcontainersBuilder<TestcontainersContainer> builder) {
		}
	}
}
