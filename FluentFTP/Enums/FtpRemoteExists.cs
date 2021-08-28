using System;

namespace FluentFTP {

	/// <summary>
	/// This enum is obsolete. Please use FtpRemoteExists instead.
	/// </summary>
	[ObsoleteAttribute("This enum is obsolete. Please use FtpRemoteExists instead.", true)]
	public enum FtpExists {
	}

	/// <summary>
	/// Defines the behavior for uploading/downloading files that already exist
	/// </summary>
	public enum FtpRemoteExists {

		/// <summary>
		/// Do not check if the file exists. A bit faster than the other options.
		/// Only use this if you are SURE that the file does not exist on the server.
		/// Otherwise it can cause the UploadFile method to hang due to filesize mismatch.
		/// </summary>
		NoCheck,

		/// <summary>
		/// Resume uploading by appending to the remote file, but don't check if it exists and add missing data.
		/// This might be required if you don't have permissions on the server to list files in the folder.
		/// Only use this if you are SURE that the file does not exist on the server otherwise it can cause the UploadFile method to hang due to filesize mismatch.
		/// </summary>
		ResumeNoCheck,

		/// <summary>
		/// Append the local file to the end of the remote file, but don't check if it exists and add missing data.
		/// This might be required if you don't have permissions on the server to list files in the folder.
		/// Only use this if you are SURE that the file does not exist on the server otherwise it can cause the UploadFile method to hang due to filesize mismatch.
		/// </summary>
		AddToEndNoCheck,

		/// <summary>
		/// Skip the file if it exists, without any more checks.
		/// </summary>
		Skip,

		/// <summary>
		/// Overwrite the file if it exists.
		/// </summary>
		Overwrite,

		/// <summary>
		/// Resume uploading by appending to the remote file if it exists.
		/// It works by checking the remote file length and adding the missing data.
		/// </summary>
		Resume,

		/// <summary>
		/// Append the local file to the end of the remote file.
		/// </summary>
		AddToEnd,

		/// <summary>
		/// Append is now renamed to Resume. Alternatively you can use AddToEnd.
		/// </summary>
		[ObsoleteAttribute("Append is now renamed to Resume to better reflect its behaviour. If you instead want to append the local file to the END of the remote file without resuming, then use AddToEnd.", true)]
		Append,
	}
}