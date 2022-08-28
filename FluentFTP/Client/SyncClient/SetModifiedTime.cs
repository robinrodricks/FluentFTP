using System;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Changes the modified time of a remote file
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="date">The new modified date/time value</param>
		public virtual void SetModifiedTime(string path, DateTime date) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			if (date == null) {
				throw new ArgumentException("Required parameter is null or blank.", "date");
			}

			path = path.GetFtpPath();

			LogFunc(nameof(SetModifiedTime), new object[] { path, date });

			FtpReply reply;

			lock (m_lock) {

				// calculate the final date string with the timezone conversion
				date = ConvertDate(date, true);
				var timeStr = date.GenerateFtpDate();

				// set modified date of a file
				if ((reply = Execute("MFMT " + timeStr + " " + path)).Success) {
				}

			}
		}

	}
}
