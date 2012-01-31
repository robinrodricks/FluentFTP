using System;

namespace System.Net.FtpClient {
	public class FtpSecurityNotAvailable : EventArgs {
		FtpControlConnection _conn = null;
		public FtpControlConnection Connection {
			get { return _conn; }
			private set { _conn = value; }
		}

		bool _cancel = false;
		public bool Cancel {
			get { return _cancel; }
			set { _cancel = value; }
		}

		public FtpSecurityNotAvailable(FtpControlConnection conn)
			: base() {
			this.Connection = conn;
		}
	}
}
