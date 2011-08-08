using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.FtpClient {
	/// <summary>
	/// The information returned from the server for a given command.
	/// </summary>
	public class FtpCommandResult {
		FtpResponseType _respType = FtpResponseType.None;
		/// <summary>
		/// The type of response received from the last command executed
		/// </summary>
		public FtpResponseType ResponseType {
			get { return _respType; }
			private set { _respType = value; }
		}

		string _respCode = null;
		/// <summary>
		/// The status code of the response
		/// </summary>
		public string ResponseCode {
			get { return _respCode; }
			private set { _respCode = value; }
		}

		string _respMessage = null;
		/// <summary>
		/// The message, if any, that the server sent with the response
		/// </summary>
		public string ResponseMessage {
			get { return _respMessage; }
			private set { _respMessage = value; }
		}

		string[] _messages = null;
		/// <summary>
		/// Other informational messages sent from the server
		/// that are not considered part of the response
		/// </summary>
		public string[] Messages {
			get { return _messages; }
			private set { _messages = value; }
		}

		/// <summary>
		/// General success or failure of the last command executed
		/// </summary>
		public bool ResponseStatus {
			get {
				if(this.ResponseCode != null) {
					int i = int.Parse(this.ResponseCode[0].ToString());

					// 1xx, 2xx, 3xx indicate success
					// 4xx, 5xx are failures
					if(i >= 1 && i <= 3) {
						return true;
					}
				}

				return false;
			}
		}

		/// <summary>
		/// Initalize object
		/// </summary>
		/// <param name="chan">The command channel this object is associated with</param>
		public FtpCommandResult(FtpCommandChannel chan) {
			this.ResponseType = chan.ResponseType;
			this.ResponseMessage = chan.ResponseMessage;
			this.ResponseCode = chan.ResponseCode;

			if(chan.Messages != null) {
				this.Messages = new string[chan.Messages.Length];
				Array.Copy(chan.Messages, this.Messages, chan.Messages.Length);
			}
		}
	}
}
