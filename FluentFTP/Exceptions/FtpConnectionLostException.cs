using System;

namespace FluentFTP.Exceptions
{
	/// <summary>
	/// This exception is thrown by FtpSocketStream
	/// </summary>
	public class FtpConnectionLostException : Exception
	{
		/// <summary>
		/// Creates a new FtpConnectionLostException
		/// </summary>
		/// <param name="innerException">The original exception</param>
		public FtpConnectionLostException(Exception innerException)
			: base("FTP connection lost", innerException)
		{
		}
	}
}
