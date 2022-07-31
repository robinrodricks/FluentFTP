using FluentFTP;
using FluentFTPServer.Model;
using FluentFTPServer.Servers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTPServer {
	public class FtpVirtualServer {

		public FtpServer Type;

		private FtpDocker Docker;

		public FtpVirtualServer(FtpServer type) {
			Type = type;

			Docker = FtpDockerIndex.Servers.FirstOrDefault(s => type == type);

			if (Docker == null) {
				throw new NotSupportedException("This server type does not have docker support! Please contribute it and add it into FtpDockerIndex!");
			}
		}

		public void Start() {
		}
		public void Stop() {
		}

	}
}
