using System;

namespace FluentFTP {

	/// <summary>
	/// Defines the behavior for uploading/downloading files that already exist
	/// </summary>
	public enum FtpExists {
		/// <summary>
		/// Do not check if the file exists. A bit faster than the other options.
		/// Only use this if you are SURE that the file does not exist on the server.
		/// Otherwise it can cause the UploadFile method to hang due to filesize mismatch.
		/// </summary>
		NoCheck,
		/// <summary>
		/// Skip the file if it exists, without any more checks.
		/// </summary>
		Skip,
		/// <summary>
		/// Overwrite the file if it exists.
		/// </summary>
		Overwrite,
		/// <summary>
		/// Append to the file if it exists, by checking the length and adding the missing data.
		/// </summary>
		Append,
		/// <summary>
		/// Append to the file, but don't check if it exists and add missing data.
		/// This might be required if you don't have permissions on the server to list files in the folder.
		/// Only use this if you are SURE that the file does not exist on the server otherwise it can cause the UploadFile method to hang due to filesize mismatch.
		/// </summary>
		AppendNoCheck
	}

}