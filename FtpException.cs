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
	/// Exception triggered on command failures
	/// </summary>
	public class FtpCommandException : FtpException {
		string _code = null;
		/// <summary>
		/// Gets the completion code associated with the response
		/// </summary>
		public string CompletionCode {
			get { return _code; }
			private set { _code = value; }
		}

		/// <summary>
		/// The type of response received from the last command executed
		/// </summary>
		public FtpResponseType ResponseType {
			get {
				if(_code != null) {
					// we only care about error types, if an exception
					// is being thrown for a successful response there
					// is a problem.
					switch(_code[0]) {
						case '4':
							return FtpResponseType.TransientNegativeCompletion;
						case '5':
							return FtpResponseType.PermanentNegativeCompletion;
					}
				}

				return FtpResponseType.None;
			}
		}

		/// <summary>
		/// Initalizes a new instance of a FtpResponseException
		/// </summary>
		/// <param name="status"></param>
		/// <param name="message"></param>
		public FtpCommandException(string code, string message)
			: base(message) {
			this.CompletionCode = code;
		}

		/// <summary>
		/// Initalizes a new instance of a FtpResponseException
		/// </summary>
		/// <param name="chan"></param>
		public FtpCommandException(FtpControlConnection chan)
			: this(chan.ResponseCode, chan.ResponseMessage) {
		}
	}

	/// <summary>
	/// Error validating the SSL certificate of an FTP server
	/// </summary>
	public class FtpInvalidCertificateException : FtpException {
		/// <summary>
		/// Initializes the exception object
		/// </summary>
		/// <param name="message">The error message</param>
		public FtpInvalidCertificateException(string message) : base(message) { }
	}

    /// <summary>
    /// Error reading the response from the server
    /// </summary>
    public class FtpResponseTimeoutException : FtpException {
        /// <summary>
        /// Initialize the exception object
        /// </summary>
        /// <param name="message">The error message</param>
        public FtpResponseTimeoutException(string message) : base(message) { }
    }
}
