using System.Text.RegularExpressions;

namespace FluentFTP.Helpers {
	/// <summary>
	/// Extension methods related to FTP tasks
	/// </summary>
	internal static class Permissions {

		/// <summary>
		/// Calculates the CHMOD value from the permissions flags
		/// </summary>
		public static void CalculateChmod(this FtpListItem item) {
			item.Chmod = CalcChmod(item.OwnerPermissions, item.GroupPermissions, item.OthersPermissions);
		}

		/// <summary>
		/// Calculates the permissions flags from the CHMOD value
		/// </summary>
		public static void CalculateUnixPermissions(this FtpListItem item, string permissions) {
			var perms = Regex.Match(permissions,
				@"[\w-]{1}(?<owner>[\w-]{3})(?<group>[\w-]{3})(?<others>[\w-]{3})",
				RegexOptions.IgnoreCase);

			if (perms.Success) {
				if (perms.Groups["owner"].Value.Length == 3) {
					if (perms.Groups["owner"].Value[0] == 'r') {
						item.OwnerPermissions |= FtpPermission.Read;
					}

					if (perms.Groups["owner"].Value[1] == 'w') {
						item.OwnerPermissions |= FtpPermission.Write;
					}

					if (perms.Groups["owner"].Value[2] == 'x' || perms.Groups["owner"].Value[2] == 's') {
						item.OwnerPermissions |= FtpPermission.Execute;
					}

					if (perms.Groups["owner"].Value[2] == 's' || perms.Groups["owner"].Value[2] == 'S') {
						item.SpecialPermissions |= FtpSpecialPermissions.SetUserID;
					}
				}

				if (perms.Groups["group"].Value.Length == 3) {
					if (perms.Groups["group"].Value[0] == 'r') {
						item.GroupPermissions |= FtpPermission.Read;
					}

					if (perms.Groups["group"].Value[1] == 'w') {
						item.GroupPermissions |= FtpPermission.Write;
					}

					if (perms.Groups["group"].Value[2] == 'x' || perms.Groups["group"].Value[2] == 's') {
						item.GroupPermissions |= FtpPermission.Execute;
					}

					if (perms.Groups["group"].Value[2] == 's' || perms.Groups["group"].Value[2] == 'S') {
						item.SpecialPermissions |= FtpSpecialPermissions.SetGroupID;
					}
				}

				if (perms.Groups["others"].Value.Length == 3) {
					if (perms.Groups["others"].Value[0] == 'r') {
						item.OthersPermissions |= FtpPermission.Read;
					}

					if (perms.Groups["others"].Value[1] == 'w') {
						item.OthersPermissions |= FtpPermission.Write;
					}

					if (perms.Groups["others"].Value[2] == 'x' || perms.Groups["others"].Value[2] == 't') {
						item.OthersPermissions |= FtpPermission.Execute;
					}

					if (perms.Groups["others"].Value[2] == 't' || perms.Groups["others"].Value[2] == 'T') {
						item.SpecialPermissions |= FtpSpecialPermissions.Sticky;
					}
				}

				CalculateChmod(item);
			}
		}

		/// <summary>
		/// Calculate the CHMOD integer value given a set of permissions.
		/// </summary>
		public static int CalcChmod(FtpPermission owner, FtpPermission group, FtpPermission other) {
			var chmod = 0;

			if (HasPermission(owner, FtpPermission.Read)) {
				chmod += 400;
			}

			if (HasPermission(owner, FtpPermission.Write)) {
				chmod += 200;
			}

			if (HasPermission(owner, FtpPermission.Execute)) {
				chmod += 100;
			}

			if (HasPermission(group, FtpPermission.Read)) {
				chmod += 40;
			}

			if (HasPermission(group, FtpPermission.Write)) {
				chmod += 20;
			}

			if (HasPermission(group, FtpPermission.Execute)) {
				chmod += 10;
			}

			if (HasPermission(other, FtpPermission.Read)) {
				chmod += 4;
			}

			if (HasPermission(other, FtpPermission.Write)) {
				chmod += 2;
			}

			if (HasPermission(other, FtpPermission.Execute)) {
				chmod += 1;
			}

			return chmod;
		}

		/// <summary>
		/// Checks if the permission value has the given flag
		/// </summary>
		private static bool HasPermission(FtpPermission owner, FtpPermission flag) {
			return (owner & flag) == flag;
		}

	}
}