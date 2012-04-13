using System;

namespace System.Net.FtpClient {
    /// <summary>
    /// SecurityNotAvailable event args
    /// </summary>
	public class FtpSecurityNotAvailable : EventArgs {
		FtpControlConnection _conn = null;
        /// <summary>
        /// The connection that triggered the event
        /// </summary>
		public FtpControlConnection Connection {
			get { return _conn; }
			private set { _conn = value; }
		}

		bool _cancel = false;
        /// <summary>
        /// Get or set a value indicating if the connection should be aborted
        /// </summary>
		public bool Cancel {
			get { return _cancel; }
			set { _cancel = value; }
		}

        /// <summary>
        /// Initalizes an instance of FtpSecurityNotAvailable event args
        /// </summary>
        /// <param name="conn"></param>
		public FtpSecurityNotAvailable(FtpControlConnection conn)
			: base() {
			this.Connection = conn;
		}
	}
}
