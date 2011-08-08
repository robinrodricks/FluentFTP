using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.FtpClient {
	/// <summary>
	/// FTP related error
	/// </summary>
	public class FtpException : Exception {
		/// <summary>
		/// Initializes the exception object
		/// </summary>
		/// <param name="message">The error message</param>
		public FtpException(string message) : base(message) { }
	}

	/// <summary>
	/// Error validating the SSL certificate of an FTP server
	/// </summary>
	public class FtpInvalidCertificateException : Exception {
		/// <summary>
		/// Initializes the exception object
		/// </summary>
		/// <param name="message">The error message</param>
		public FtpInvalidCertificateException(string message) : base(message) { }
	}
}
