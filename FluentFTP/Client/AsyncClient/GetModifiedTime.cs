using System;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Gets the modified time of a remote file asynchronously
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		public async Task<DateTime> GetModifiedTime(string path, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();

			LogFunction(nameof(GetModifiedTime), new object[] { path });

			var date = DateTime.MinValue;
			FtpReply reply;

			// get modified date of a file
			if ((reply = await Execute("MDTM " + path, token)).Success) {
				date = reply.Message.ParseFtpDate(this);
				date = ConvertDate(date);
			}

			return date;
		}

	}
}
