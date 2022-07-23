using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class GetListingWithLinksExample {

		public static void GetListing() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				// get a listing of the files with their modified date & size
				foreach (var item in conn.GetListing(conn.GetWorkingDirectory(),
				FtpListOption.Modify | FtpListOption.Size)) {
					switch (item.Type) {
						case FtpObjectType.Directory:
							break;

						case FtpObjectType.File:
							break;

						case FtpObjectType.Link:

							// dereference symbolic links
							if (item.LinkTarget != null) {
								// see the DereferenceLink() example
								// for more details about resolving links.
								item.LinkObject = conn.DereferenceLink(item);

								if (item.LinkObject != null) {
									// switch (item.LinkObject.Type)...
								}
							}

							break;
					}
				}

				// get a listing of the files with their modified date & size, and automatically dereference links
				foreach (var item in conn.GetListing(conn.GetWorkingDirectory(),
					FtpListOption.Modify | FtpListOption.Size | FtpListOption.DerefLinks)) {
					switch (item.Type) {
						case FtpObjectType.Directory:
							break;

						case FtpObjectType.File:
							break;

						case FtpObjectType.Link:
							if (item.LinkObject != null) {
								// switch (item.LinkObject.Type)...
							}

							break;
					}
				}
			}
		}

		public static async Task GetListingAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				// get a listing of the files with their modified date & size
				foreach (var item in await conn.GetListingAsync(conn.GetWorkingDirectory(),
				FtpListOption.Modify | FtpListOption.Size, token)) {
					switch (item.Type) {
						case FtpObjectType.Directory:
							break;

						case FtpObjectType.File:
							break;

						case FtpObjectType.Link:

							// dereference symbolic links
							if (item.LinkTarget != null) {
								// see the DereferenceLink() example
								// for more details about resolving links.
								item.LinkObject = conn.DereferenceLink(item);

								if (item.LinkObject != null) {
									// switch (item.LinkObject.Type)...
								}
							}

							break;
					}
				}

				// get a listing of the files with their modified date & size, and automatically dereference links
				foreach (var item in await conn.GetListingAsync(conn.GetWorkingDirectory(),
					FtpListOption.Modify | FtpListOption.Size | FtpListOption.DerefLinks, token)) {
					switch (item.Type) {
						case FtpObjectType.Directory:
							break;

						case FtpObjectType.File:
							break;

						case FtpObjectType.Link:
							if (item.LinkObject != null) {
								// switch (item.LinkObject.Type)...
							}

							break;
					}
				}
			}
		}

	}
}