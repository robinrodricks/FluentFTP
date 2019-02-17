using System;

namespace FluentFTP
{

    /// <summary>
    /// Defines the type of encryption to use
    /// </summary>
    public enum LocalFileExists
    {
        /// <summary>
        /// Overwrite the file if it exists.
        /// </summary>
        Overwrite,
        /// <summary>
        /// Append to the file if it exists, by checking the length and adding the missing data. If it doesent exits a new file will be created
        /// </summary>
        Append,
    }
}