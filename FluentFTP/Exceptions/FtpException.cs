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
		public FtpException(string message) : base(message) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FtpException"/> class with an inner exception.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
		public FtpException(string message, Exception innerException) : base(message, innerException) {
		}

#if !CORE
		/// <summary>
		/// Must be implemented so every Serializer can Deserialize the Exception
		/// </summary>
		protected FtpException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}

#endif
	}
}