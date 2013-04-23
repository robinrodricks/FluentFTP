using System;

namespace System.Net.FtpClient {
    /// <summary>
    /// Added for the MoQ unit testing framework
    /// </summary>
    public interface IFtpReply {
        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        string Code { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        string InfoMessages { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        string Message { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        bool Success { get; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        FtpResponseType Type { get; }
    }
}
