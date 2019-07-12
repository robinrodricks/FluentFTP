using System;

namespace FluentFTP.Exceptions
{
	/// <summary>
	/// This exception is thrown by FtpSocketStream.
	/// </summary>
	public class FtpMissingSocketException : Exception
	{
		/// <summary>
		/// Creates a new FtpMissingSocketException.
		/// </summary>
		/// <param name="innerException">The original exception.</param>
		public FtpMissingSocketException(Exception innerException)
			: base("Socket is missing", innerException)
		{
		}
	}
}
