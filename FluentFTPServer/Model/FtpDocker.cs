using FluentFTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTPServer.Model {
	internal class FtpDocker {

		public FtpServer Type;
		public string DockerName;
		public string DockerFolder;
		public string RunCommand;
		public string FtpUser;
		public string FtpPass;

	}
}
