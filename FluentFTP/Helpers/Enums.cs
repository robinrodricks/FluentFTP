namespace FluentFTP.Helpers {
	/// <summary>
	/// Extension methods related to FTP tasks
	/// </summary>
	public static class Enums {

		/// <summary>
		/// Validates that the FtpError flags set are not in an invalid combination.
		/// </summary>
		/// <param name="options">The error handling options set</param>
		/// <returns>True if a valid combination, otherwise false</returns>
		public static bool IsValidCombination(this FtpError options) {
			return options != (FtpError.Stop | FtpError.Throw) &&
				   options != (FtpError.Throw | FtpError.Stop | FtpError.DeleteProcessed);
		}

		/// <summary>
		/// Checks if the operation was successful or skipped (indicating success).
		/// </summary>
		public static bool IsSuccess(this FtpStatus status) {
			return status is FtpStatus.Success or FtpStatus.Skipped;
		}

		/// <summary>
		/// Checks if the operation has failed.
		/// </summary>
		public static bool IsFailure(this FtpStatus status) {
			return status == FtpStatus.Failed;
		}

	}
}
