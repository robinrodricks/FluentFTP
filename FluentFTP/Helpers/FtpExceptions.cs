using System;
#if !CORE
using System.Runtime.Serialization;
#endif

namespace FluentFTP {
	/// <summary>
	/// FTP related error
	/// </summary>
#if !CORE
	[Serializable]
#endif
	public class FtpException : Exception {

		/// <summary>
		/// Initializes a new instance of the <see cref="FtpException"/> class.
		/// </summary>
		/// <param name="message">The error message</param>
		public FtpException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FtpException"/> class with an inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
		public FtpException(string message, Exception innerException) : base(message, innerException) { }

#if !CORE
		/// <summary>
		/// Must be implemented so every Serializer can Deserialize the Exception
		/// </summary>
		protected FtpException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
	}

	/// <summary>
	/// Exception triggered on command failures
	/// </summary>
#if !CORE
	[Serializable]
#endif
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
				if (_code != null) {
					// we only care about error types, if an exception
					// is being thrown for a successful response there
					// is a problem.
					switch (_code[0]) {
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
		/// Initializes a new instance of a FtpResponseException
		/// </summary>
		/// <param name="code">Status code</param>
		/// <param name="message">Associated message</param>
		public FtpCommandException(string code, string message)
			: base(message) {
			CompletionCode = code;
		}

		/// <summary>
		/// Initializes a new instance of a FtpResponseException
		/// </summary>
		/// <param name="reply">The FtpReply to build the exception from</param>
		public FtpCommandException(FtpReply reply)
			: this(reply.Code, reply.ErrorMessage) {
		}

#if !CORE
		/// <summary>
		/// Must be implemented so every Serializer can Deserialize the Exception
		/// </summary>
		protected FtpCommandException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
	}

	/// <summary>
	/// Exception is thrown when encryption could not be negotiated by the server
	/// </summary>
#if !CORE
	[Serializable]
#endif
	public class FtpSecurityNotAvailableException : FtpException {
		/// <summary>
		/// Default constructor
		/// </summary>
		public FtpSecurityNotAvailableException()
			: base("Security is not available on the server.") {
		}

		/// <summary>
		/// Custom error message
		/// </summary>
		/// <param name="message">Error message</param>
		public FtpSecurityNotAvailableException(string message)
			: base(message) {
		}

#if !CORE
		/// <summary>
		/// Must be implemented so every Serializer can Deserialize the Exception
		/// </summary>
		protected FtpSecurityNotAvailableException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
	}
}