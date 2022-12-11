using System;
#if NETFRAMEWORK
using System.Runtime.Serialization;
#endif

namespace FluentFTP {

	/// <summary>
	/// FtpProtocolUnsupportedException
	/// </summary>
#if NETFRAMEWORK
	[Serializable]
#endif
	public class FtpProtocolUnsupportedException : FtpException {

		/// <summary>
		/// FtpProtocolUnsupportedException
		/// </summary>
		/// <param name="message">Error message</param>
		public FtpProtocolUnsupportedException(string message)
			: base(message) {
		}
	}
}
