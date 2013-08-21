using System;

namespace System.Net.FtpClient {
    /// <summary>
    /// FTP related error
    /// </summary>
    public class FtpException : Exception {
        /// <summary>
        /// Initializes the exception object
        /// </summary>
        /// <param name="message">The error message</param>
        public FtpException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception triggered on command failures
    /// </summary>
    public class FtpCommandException : FtpException {
        string _code = null;
        /// <summary>
        /// Gets the completion code associated with the response
        /// </summary>
        public string CompletionCode {
            get { return _code; }
            private set { _code = value; }
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
        /// Initalizes a new instance of a FtpResponseException
        /// </summary>
        /// <param name="code">Status code</param>
        /// <param name="message">Associated message</param>
        public FtpCommandException(string code, string message)
            : base(message) {
            CompletionCode = code;
        }

        /// <summary>
        /// Initalizes a new instance of a FtpResponseException
        /// </summary>
        /// <param name="reply">The FtpReply to build the exception from</param>
        public FtpCommandException(FtpReply reply)
            : this(reply.Code, reply.ErrorMessage) {
        }
    }

    /// <summary>
    /// Exception is thrown when encryption could not be negotiated by the server
    /// </summary>
    public class FtpSecurityNotAvailableException : FtpException {
        /// <summary>
        /// Default constructor
        /// </summary>
        public FtpSecurityNotAvailableException()
            : base("Security is not available on the server.") {
        }

        /// <summary>
        /// Custom error message
        /// </summary>
        /// <param name="message">Error message</param>
        public FtpSecurityNotAvailableException(string message)
            : base(message) {
        }
    }
}
