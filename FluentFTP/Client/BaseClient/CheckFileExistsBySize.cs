using FluentFTP.Helpers;
using FluentFTP.Client.Modules;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Try using the SIZE command to check if file exists
		/// </summary>
		/// <param name="sizeReply"></param>
		/// <returns></returns>
		protected bool? CheckFileExistsBySize(FtpSizeReply sizeReply) {

			// file surely exists
			if (sizeReply.Reply.Code[0] == '2') {
				return true;
			}

			// file surely does not exist
			if (sizeReply.Reply.Code[0] == '5' && sizeReply.Reply.Message.ContainsAnyCI(ServerStringModule.fileNotFound)) {
				return false;
			}

			// Fix #518: This check is too broad and must be disabled, need to fallback to MDTM or NLST instead.
			// Fix #179: Add a generic check to since server returns 550 if file not found or no access to file.
			/*if (sizeReply.Reply.Code.Substring(0, 3) == "550") {
				return false;
			}*/

			// fallback to MDTM or NLST
			return null;
		}

	}
}