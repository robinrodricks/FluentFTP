using System;
#if NETFRAMEWORK
using System.Runtime.Serialization;
#endif

namespace FluentFTP.Exceptions {
	/// <summary>
	/// Exception thrown by FtpListParser when parsing of FTP directory listing fails.
	/// </summary>
#if NETFRAMEWORK
	[Serializable]
#endif
	public class FtpListParseException : FtpException {
		/// <summary>
		/// Creates a new FtpListParseException.
		/// </summary>
		public FtpListParseException()
			: base("Cannot parse file listing!") {
		}

#if NETFRAMEWORK
		/// <summary>
		/// Must be implemented so every Serializer can Deserialize the Exception
		/// </summary>
		protected FtpListParseException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}

#endif
	}
}