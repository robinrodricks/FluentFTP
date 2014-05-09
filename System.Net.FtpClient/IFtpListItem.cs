namespace System.Net.FtpClient
{
    /// <summary>
    /// Represents a file system object on the server
    /// </summary>
    /// <example><code source="..\Examples\CustomParser.cs" lang="cs" /></example>
    public interface IFtpListItem
    {
        /// <summary>
        /// Gets the type of file system object. This property can be
        /// set however this functionality is intended to be done by
        /// custom parsers.
        /// </summary>
        FtpFileSystemObjectType Type { get; set; }

        /// <summary>
        /// Gets the full path name to the object. This property can be
        /// set however this functionality is intended to be done by
        /// custom parsers.
        /// </summary>
        string FullName { get; set; }

        /// <summary>
        /// Gets the name of the object. This property can be
        /// set however this functionality is intended to be done by
        /// custom parsers.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets the target a symbolic link points to. This property can be
        /// set however this functionality is intended to be done by
        /// custom parsers.
        /// </summary>
        string LinkTarget { get; set; }

        /// <summary>
        /// Gets the object the LinkTarget points to. This property is null unless pass the
        /// FtpListOption.DerefLink flag in which case GetListing() will try to resolve
        /// the target itself.
        /// </summary>
        FtpListItem LinkObject { get; set; }

        /// <summary>
        /// Gets the last write time of the object. This property can be
        /// set however this functionality is intended to be done by
        /// custom parsers.
        /// </summary>
        DateTime Modified { get; set; }

        /// <summary>
        /// Gets the created date of the object. This property can be
        /// set however this functionality is intended to be done by
        /// custom parsers.
        /// </summary>
        DateTime Created { get; set; }

        /// <summary>
        /// Gets the size of the object. This property can be
        /// set however this functionality is intended to be done by
        /// custom parsers.
        /// </summary>
        long Size { get; set; }

        /// <summary>
        /// Gets special UNIX permissions such as Stiky, SUID and SGID. This property can be
        /// set however this functionality is intended to be done by
        /// custom parsers.
        /// </summary>
        FtpSpecialPermissions SpecialPermissions { get; set; }

        /// <summary>
        /// Gets the owner permissions. This property can be
        /// set however this functionality is intended to be done by
        /// custom parsers.
        /// </summary>
        FtpPermission OwnerPermissions { get; set; }

        /// <summary>
        /// Gets the group permissions. This property can be
        /// set however this functionality is intended to be done by
        /// custom parsers.
        /// </summary>
        FtpPermission GroupPermissions { get; set; }

        /// <summary>
        /// Gets the others permissions. This property can be
        /// set however this functionality is intended to be done by
        /// custom parsers.
        /// </summary>
        FtpPermission OthersPermissions { get; set; }

        /// <summary>
        /// Gets the input string that was parsed to generate the
        /// values in this object. This property can be
        /// set however this functionality is intended to be done by
        /// custom parsers.
        /// </summary>
        string Input { get; }

        /// <summary>
        /// Returns a string representation of this object and its properties
        /// </summary>
        /// <returns>A string value</returns>
        string ToString();
    }
}