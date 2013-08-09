using System;
using System.IO;
using System.Text.RegularExpressions;

namespace System.Net.FtpClient {
    /// <summary>
    /// Represents a reply to an event on the server
    /// </summary>
    public struct FtpReply : IFtpReply {
        /// <summary>
        /// The type of response received from the last command executed
        /// </summary>
        public FtpResponseType Type {
            get {
                int code;

                if (Code != null && Code.Length > 0 &&
                    int.TryParse(Code[0].ToString(), out code)) {
                    return (FtpResponseType)code;
                }

                return FtpResponseType.None;
            }
        }

        string m_respCode;
        /// <summary>
        /// The status code of the response
        /// </summary>
        public string Code {
            get { 
                return m_respCode; 
            }
            set { 
                m_respCode = value; 
            }
        }

        string m_respMessage;
        /// <summary>
        /// The message, if any, that the server sent with the response
        /// </summary>
        public string Message {
            get { 
                return m_respMessage; 
            }
            set { 
                m_respMessage = value; 
            }
        }

        string m_infoMessages;
        /// <summary>
        /// Informational messages sent from the server
        /// </summary>
        public string InfoMessages {
            get { 
                return m_infoMessages; 
            }
            set { 
                m_infoMessages = value; 
            }
        }

        /// <summary>
        /// General success or failure of the last command executed
        /// </summary>
        public bool Success {
            get {
                if (Code != null && Code.Length > 0) {
                    int i;

                    // 1xx, 2xx, 3xx indicate success
                    // 4xx, 5xx are failures
                    if (int.TryParse(Code[0].ToString(), out i) && i >= 1 && i <= 3) {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the error message including any informational output
        /// that was sent by the server. Sometimes the final response
        /// line doesn't contain anything informative as to what was going
        /// on with the server. Instead it may send information messages so
        /// in an effort to give as meaningful as a response as possible
        /// the informational messages will be included in the error.
        /// </summary>
        public string ErrorMessage {
            get {
                string message = "";

                if (Success) {
                    return message;
                }

                if (InfoMessages != null && InfoMessages.Length > 0) {
                    foreach (string s in InfoMessages.Split('\n')) {
                        string m = Regex.Replace(s, "^[0-9]{3}-", "");
                        message += string.Format("{0}; ", m.Trim());
                    }
                }

                message += Message;

                return message;
            }
        }
    }
}
