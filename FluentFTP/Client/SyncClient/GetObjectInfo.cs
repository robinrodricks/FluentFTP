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
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			path = path.GetFtpPath();

			LogFunc(nameof(GetObjectInfo), new object[] { path, dateModified });

			FtpReply reply;
			string[] res;

			var supportsMachineList = HasFeature(FtpCapability.MLSD);

			FtpListItem result = null;

			if (supportsMachineList) {
				// USE MACHINE LISTING TO GET INFO FOR A SINGLE FILE

				if ((reply = Execute("MLST " + path)).Success) {
					res = reply.InfoMessages.Split('\n');
					if (res.Length > 1) {
						var info = new StringBuilder();

						for (var i = 1; i < res.Length; i++) {
							info.Append(res[i]);
						}

						result = m_listParser.ParseSingleLine(null, info.ToString(), m_capabilities, true);
					}
				}
				else {
					LogStatus(FtpTraceLevel.Warn, "Failed to get object info for path " + path + " with error " + reply.ErrorMessage);
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

				LogStatus(FtpTraceLevel.Warn, "Failed to get object info for path " + path + " since MLST not supported and GetListing() fails to list file/folder.");
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

#if ASYNC
		/// <summary>
		/// Return information about a remote file system object asynchronously. 
		/// </summary>
		/// <remarks>
		/// You should check the <see cref="Capabilities"/> property for the <see cref="FtpCapability.MLSD"/> 
		/// flag before calling this method. Failing to do so will result in an InvalidOperationException
		/// being thrown when the server does not support machine listings. Returns null if the server response can't
		/// be parsed or the server returns a failure completion code. The error for a failure
		/// is logged with FtpTrace. No exception is thrown on error because that would negate
		/// the usefulness of this method for checking for the existence of an object.</remarks>
		/// <param name="path">Path of the item to retrieve information about</param>
		/// <param name="dateModified">Get the accurate modified date using another MDTM command</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <exception cref="InvalidOperationException">Thrown if the server does not support this Capability</exception>
		/// <returns>A <see cref="FtpListItem"/> if the command succeeded, or null if there was a problem.</returns>
		public async Task<FtpListItem> GetObjectInfoAsync(string path, bool dateModified = false, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			path = path.GetFtpPath();

			LogFunc(nameof(GetObjectInfo), new object[] { path, dateModified });

			FtpReply reply;
			string[] res;

			var supportsMachineList = HasFeature(FtpCapability.MLSD);

			FtpListItem result = null;

			if (supportsMachineList) {
				// USE MACHINE LISTING TO GET INFO FOR A SINGLE FILE

				if ((reply = await ExecuteAsync("MLST " + path, token)).Success) {
					res = reply.InfoMessages.Split('\n');
					if (res.Length > 1) {
						var info = new StringBuilder();

						for (var i = 1; i < res.Length; i++) {
							info.Append(res[i]);
						}

						result = m_listParser.ParseSingleLine(null, info.ToString(), m_capabilities, true);
					}
				}
				else {
					LogStatus(FtpTraceLevel.Warn, "Failed to get object info for path " + path + " with error " + reply.ErrorMessage);
				}
			}
			else {
				// USE GETLISTING TO GET ALL FILES IN DIR .. SLOWER BUT AT LEAST IT WORKS

				var dirPath = path.GetFtpDirectoryName();
				var dirItems = await GetListingAsync(dirPath, token);

				foreach (var dirItem in dirItems) {
					if (dirItem.FullName == path) {
						result = dirItem;
						break;
					}
				}

				LogStatus(FtpTraceLevel.Warn, "Failed to get object info for path " + path + " since MLST not supported and GetListing() fails to list file/folder.");
			}

			// Get the accurate date modified using another MDTM command
			if (result != null && dateModified && HasFeature(FtpCapability.MDTM)) {
				var alternativeModifiedDate = await GetModifiedTimeAsync(path, token);
				if (alternativeModifiedDate != default) {
					result.Modified = alternativeModifiedDate;
				}
			}

			return result;
		}
#endif

	}

}
