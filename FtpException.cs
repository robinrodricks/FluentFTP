using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.FtpClient {
	public class FtpException : Exception {
		public FtpException(string message) : base(message) { }
	}

	public class FtpInvalidCertificateException : Exception {
		public FtpInvalidCertificateException(string message) : base(message) { }
	}
}
