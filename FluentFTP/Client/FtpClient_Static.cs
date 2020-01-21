using System;
using System.IO;
using System.Net;

namespace FluentFTP {
	public partial class FtpClient : IDisposable {
		/// <summary>
		/// Connects to the specified URI. If the path specified by the URI ends with a
		/// / then the working directory is changed to the path specified.
		/// </summary>
		/// <param name="uri">The URI to parse</param>
		/// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
		/// <returns>FtpClient object</returns>
		public static FtpClient Connect(Uri uri, bool checkcertificate) {
			var cl = new FtpClient();

			if (uri == null) {
				throw new ArgumentException("Invalid URI object");
			}

			switch (uri.Scheme.ToLower()) {
				case "ftp":
				case "ftps":
					break;

				default:
					throw new UriFormatException("The specified URI scheme is not supported. Please use ftp:// or ftps://");
			}

			cl.Host = uri.Host;
			cl.Port = uri.Port;

			if (uri.UserInfo != null && uri.UserInfo.Length > 0) {
				if (uri.UserInfo.Contains(":")) {
					var parts = uri.UserInfo.Split(':');

					if (parts.Length != 2) {
						throw new UriFormatException("The user info portion of the URI contains more than 1 colon. The username and password portion of the URI should be URL encoded.");
					}

					cl.Credentials = new NetworkCredential(DecodeUrl(parts[0]), DecodeUrl(parts[1]));
				}
				else {
					cl.Credentials = new NetworkCredential(DecodeUrl(uri.UserInfo), "");
				}
			}
			else {
				// if no credentials were supplied just make up
				// some for anonymous authentication.
				cl.Credentials = new NetworkCredential("ftp", "ftp");
			}

			cl.ValidateCertificate += new FtpSslValidation(delegate (FtpClient control, FtpSslValidationEventArgs e) {
				if (e.PolicyErrors != System.Net.Security.SslPolicyErrors.None && checkcertificate) {
					e.Accept = false;
				}
				else {
					e.Accept = true;
				}
			});

			cl.Connect();

			if (uri.PathAndQuery != null && uri.PathAndQuery.EndsWith("/")) {
				cl.SetWorkingDirectory(uri.PathAndQuery);
			}

			return cl;
		}

		/// <summary>
		/// Connects to the specified URI. If the path specified by the URI ends with a
		/// / then the working directory is changed to the path specified.
		/// </summary>
		/// <param name="uri">The URI to parse</param>
		/// <returns>FtpClient object</returns>
		public static FtpClient Connect(Uri uri) {
			return Connect(uri, true);
		}

		/// <summary>
		/// Calculate you public internet IP using the ipify service. Returns null if cannot be calculated.
		/// </summary>
		/// <returns>Public IP Address</returns>
		public static string GetPublicIP() {
#if NETFX
			try {
				var request = WebRequest.Create("https://api.ipify.org/");
				request.Method = "GET";

				using (var response = request.GetResponse()) {
					using (var stream = new StreamReader(response.GetResponseStream())) {
						return stream.ReadToEnd();
					}
				}
			}
			catch (Exception) {
			}

#endif
			return null;
		}
	}
}