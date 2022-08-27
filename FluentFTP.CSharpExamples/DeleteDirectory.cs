using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class DeleteDirectoryExample {

		public static void DeleteDirectory() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				// Remove the directory and all files and subdirectories inside it
				conn.DeleteDirectory("/path/to/directory");
			}
		}

		public static async Task DeleteDirectoryAsync() {
			var token = new CancellationToken();
			using (var conn = new AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.Connect(token);

				// Remove the directory and all files and subdirectories inside it
				await conn.DeleteDirectory("/path/to/directory", token);
			}
		}
	}
}