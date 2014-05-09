namespace System.Net.FtpClient
{
    /// <summary>
    /// Represents a reply to an event on the server
    /// </summary>
    public interface IFtpReply
    {
        /// <summary>
        /// The type of response received from the last command executed
        /// </summary>
        FtpResponseType Type { get; }

        /// <summary>
        /// The status code of the response
        /// </summary>
        string Code { get; set; }

        /// <summary>
        /// The message, if any, that the server sent with the response
        /// </summary>
        string Message { get; set; }

        /// <summary>
        /// Informational messages sent from the server
        /// </summary>
        string InfoMessages { get; set; }

        /// <summary>
        /// General success or failure of the last command executed
        /// </summary>
        bool Success { get; }

        /// <summary>
        /// Gets the error message including any informational output
        /// that was sent by the server. Sometimes the final response
        /// line doesn't contain anything informative as to what was going
        /// on with the server. Instead it may send information messages so
        /// in an effort to give as meaningful as a response as possible
        /// the informational messages will be included in the error.
        /// </summary>
        string ErrorMessage { get; }
    }
}