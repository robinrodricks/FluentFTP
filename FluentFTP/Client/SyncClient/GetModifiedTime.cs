using System;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Gets the modified time of a remote file.
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		public virtual DateTime GetModifiedTime(string path) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();

			LogFunction(nameof(GetModifiedTime), new object[] { path });

			var date = DateTime.MinValue;
			FtpReply reply;

			// get modified date of a file
			if ((reply = Execute("MDTM " + path)).Success) {
				date = reply.Message.ParseFtpDate(this);
				date = ConvertDate(date);
			}

			return date;
		}

	}
}
