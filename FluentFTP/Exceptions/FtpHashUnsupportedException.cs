using System;
using System.Collections.Generic;
using System.Text;
using FluentFTP.Helpers.Hashing;
#if NETFRAMEWORK
using System.Runtime.Serialization;
#endif

namespace FluentFTP.Exceptions {

	/// <summary>
	/// Exception is thrown when the required hash algorithm is unsupported by the server.
	/// </summary>
#if NETFRAMEWORK
	[Serializable]
#endif
	public class FtpHashUnsupportedException : FtpException {

		private FtpHashAlgorithm _algo = FtpHashAlgorithm.NONE;

		/// <summary>
		/// Gets the unsupported hash algorithm
		/// </summary>
		public FtpHashAlgorithm Algorithm {
			get => _algo;
			private set => _algo = value;
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public FtpHashUnsupportedException()
			: base("Your FTP server does not support the HASH command or any of the algorithm-specific commands. Use a better FTP server software or install a hashing/checksum module onto your server.") {

		}

		/// <summary>
		/// Algorithm-specific constructor
		/// </summary>
		public FtpHashUnsupportedException(FtpHashAlgorithm algo, string specialCommands)
			: base("Hash algorithm " + algo.PrintToString() + " is unsupported by your server using the HASH command or the " +
				  specialCommands + " command(s). " +
				  "Use another algorithm or use FtpHashAlgorithm.NONE to select the first available algorithm.") {

			Algorithm = algo;
		}

#if NETFRAMEWORK
		/// <summary>
		/// Must be implemented so every Serializer can Deserialize the Exception
		/// </summary>
		protected FtpHashUnsupportedException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}

#endif
	}
}
