using FluentFTP.Helpers;
using System;
#if NETFRAMEWORK
using System.Runtime.Serialization;
#endif

namespace FluentFTP.Exceptions {
	/// <summary>
	/// Exception is thrown by FtpClient/AsyncFtpClient when the primary file or folder to be downloaded is missing.
	/// </summary>
#if NETFRAMEWORK
	[Serializable]
#endif
	public class FtpMissingObjectException : FtpException {

		/// <summary>
		/// Gets the type of file system object.
		/// </summary>
		public FtpObjectType Type { get; set; }

		/// <summary>
		/// Gets the full path name to the file or folder.
		/// </summary>
		public string FullPath { get; set; }

		/// <summary>
		/// Gets the name of the file or folder. Does not include the full path.
		/// </summary>
		public string Name {
			get {
				if (FullPath != null) {
					return FullPath.GetFtpFileName();
				}
				return null;
			}
		}


		/// <summary>
		/// Creates a new FtpMissingObjectException.
		/// </summary>
		/// <param name="innerException">The original exception.</param>
		public FtpMissingObjectException(string message, Exception innerException, string fullPath, FtpObjectType type)
			: base(message, innerException) {
			this.FullPath = fullPath;
			this.Type = type;
		}

#if NETFRAMEWORK
		/// <summary>
		/// Must be implemented so every Serializer can Deserialize the Exception
		/// </summary>
		protected FtpMissingObjectException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}

#endif
	}
}