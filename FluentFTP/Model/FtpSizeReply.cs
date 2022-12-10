using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP {

	/// <summary>
	/// Reply from a SIZE command
	/// </summary>
	public class FtpSizeReply {

		/// <summary>
		/// The returned file size
		/// </summary>
		public long FileSize { get; set; }

		/// <summary>
		/// The reply we got
		/// </summary>
		public FtpReply Reply { get; set; }

	}
}
