using System;
using System.IO;
using System.Text.RegularExpressions;

namespace System.Net.FtpClient {
    /// <summary>
    /// Represents a reply to an event on the server
    /// </summary>
    public class FtpReply {
        /// <summary>
        /// The type of response received from the last command executed
        /// </summary>
        public FtpResponseType Type {
            get {
                int code;

                if (this.Code != null && this.Code.Length > 0 && 
                    int.TryParse(this.Code[0].ToString(), out code)) {
                    return (FtpResponseType)code;
                }

                return FtpResponseType.None;
            }
        }

        string _respCode = null;
        /// <summary>
        /// The status code of the response
        /// </summary>
        public string Code {
            get { return _respCode; }
            set { _respCode = value; }
        }

        string _respMessage = null;
        /// <summary>
        /// The message, if any, that the server sent with the response
        /// </summary>
        public string Message {
            get { return _respMessage; }
            set { _respMessage = value; }
        }

        string _infoMessages = "";
        /// <summary>
        /// Informational messages sent from the server
        /// </summary>
        public string InfoMessages {
            get { return _infoMessages; }
            set { _infoMessages = value; }
        }

        /// <summary>
        /// General success or failure of the last command executed
        /// </summary>
        public bool Success {
            get {
                if (this.Code != null) {
                    int i = int.Parse(this.Code[0].ToString());

                    // 1xx, 2xx, 3xx indicate success
                    // 4xx, 5xx are failures
                    if (i >= 1 && i <= 3) {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Creates a new instance of the FtpReply class
        /// </summary>
        public FtpReply() { }
    }
}
