
namespace FluentFTP.Helpers {
	/// <summary>
	/// Extension methods related to FTP commands.
	/// </summary>
	public static class CommandSanitizer {

		/// <summary>
		/// Converts the specified command into a valid FTP command.
		/// Multiline commands are stripped and only the first line of the command is retained.
		/// </summary>
		public static string Sanitize(string command) {
			if (command == null || command.Length == 0) {
				return command;
			}

			// FIX: Prevent multiline FTP commands
			command = command.BeforeFirst('\r', true).BeforeFirst('\n', true);

			return command;
		}

	}
}
