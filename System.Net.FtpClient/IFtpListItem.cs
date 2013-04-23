using System;
namespace System.Net.FtpClient {
    /// <summary>
    /// Added for the MoQ unit testing framework
    /// </summary>
    public interface IFtpListItem {
        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        DateTime Created { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        string FullName { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        FtpPermission GroupPermissions { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        string Input { get; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        DateTime Modified { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        FtpPermission OthersPermissions { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        FtpPermission OwnerPermissions { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        long Size { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        FtpSpecialPermissions SpecialPermissions { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        FtpFileSystemObjectType Type { get; set; }
    }
}
