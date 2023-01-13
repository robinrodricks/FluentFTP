using System;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Exceptions;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Gets the modified time of a remote file asynchronously
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="date">The new modified date/time value</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public async Task SetModifiedTime(string path, DateTime date, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();

			LogFunction(nameof(SetModifiedTime), new object[] { path, date });

			FtpReply reply;

			// calculate the final date string with the timezone conversion
			date = ConvertDate(date, true);
			var timeStr = date.GenerateFtpDate();

			// set modified date of a file
			if (HasFeature(FtpCapability.MFMT)) {
				if ((reply = await Execute("MFMT " + timeStr + " " + path, token)).Success) {
				}
			}
			else if (HasFeature(FtpCapability.MDTM)) {
				if ((reply = await Execute("MDTM " + timeStr + " " + path, token)).Success) {
				}
			}
			else {
				throw new FtpException("No time setting command available - see FEAT response");
			}

			// TODO: Consider also supporting SITE UTIME.
			// Advantages:
			//	Uses UTC, no time zone concerns
			// Disadvantages:
			//  Some servers do not advertise this command in FEAT response,
			//  need to test the availability of this command
			//  Some servers do not support it correctly or in different formats

		}

	}
}
