using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP {
	internal class FtpSizeReply {

		public long FileSize { get; set; }

		public FtpReply Reply;

	}
}
