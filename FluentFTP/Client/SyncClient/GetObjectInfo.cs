using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using FluentFTP.Exceptions;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using FluentFTP.Client.Modules;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Returns information about a file system object. Returns null if the server response can't
		/// be parsed or the server returns a failure completion code. The error for a failure
		/// is logged with FtpTrace. No exception is thrown on error because that would negate
		/// the usefulness of this method for checking for the existence of an object.
		/// </summary>
		/// <param name="path">The path of the file or folder</param>
		/// <param name="dateModified">Get the accurate modified date using another MDTM command</param>
		/// <returns>A FtpListItem object</returns>
		public FtpListItem GetObjectInfo(string path, bool dateModified = false) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();

			LogFunction(nameof(GetObjectInfo), new object[] { path, dateModified });

			FtpReply reply;

			var supportsMachineList = HasFeature(FtpCapability.MLSD);

			FtpListItem result = null;

			if (supportsMachineList) {
				// USE MACHINE LISTING TO GET INFO FOR A SINGLE FILE

				if ((reply = Execute("MLST " + path)).Success) {
					if (reply.InfoMessages != null) {
						string[] res = reply.InfoMessages.Split('\n');
						if (res.Length > 1) {
							var info = new StringBuilder();

							for (var i = 1; i < res.Length; i++) {
								info.Append(res[i]);
							}

							result = CurrentListParser.ParseSingleLine(null, info.ToString(), m_capabilities, true);
						}
					}
					else {
						LogWithPrefix(FtpTraceLevel.Warn, "Failed to get object info for path " + path + " with error: Null response");
					}
				}
				else {
					LogWithPrefix(FtpTraceLevel.Warn, "Failed to get object info for path " + path + " with error " + reply.ErrorMessage);
				}
			}
			else {
				// USE GETLISTING TO GET ALL FILES IN DIR .. SLOWER BUT AT LEAST IT WORKS

				var dirPath = path.GetFtpDirectoryName();
				var dirItems = GetListing(dirPath);

				foreach (var dirItem in dirItems) {
					if (dirItem.FullName == path) {
						result = dirItem;
						break;
					}
				}

				if (result == null) {
					LogWithPrefix(FtpTraceLevel.Warn, "Failed to get object info for path " + path + " using \"GetListing(...)\", MLST not supported.");
				}
			}

			// Get the accurate date modified using another MDTM command
			if (result != null && dateModified && HasFeature(FtpCapability.MDTM)) {
				var alternativeModifiedDate = GetModifiedTime(path);
				if (alternativeModifiedDate != default) {
					result.Modified = alternativeModifiedDate;
				}
			}

			return result;
		}

	}

}
