using System;
#if !CORE
using System.Runtime.Serialization;
#endif

namespace FluentFTP {

	/// <summary>
	/// Exception triggered on FTP command failures
	/// </summary>
#if !CORE
	[Serializable]
#endif
	public class FtpCommandException : FtpException {
		private string _code = null;

		/// <summary>
		/// Gets the completion code associated with the response
		/// </summary>
		public string CompletionCode {
			get => _code;
			private set => _code = value;
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
		protected FtpCommandException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}

#endif
	}
}
