using System.Threading;
using System.Threading.Tasks;

using FluentFTP.Client.Modules;
using FluentFTP.Model.Functions;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and connects to the first successful profile.
		/// Returns the FtpProfile if the connection succeeded, or null if it failed.
		/// It will throw exceptions for permanent failures like invalid host or invalid credentials.
		/// </summary>
		public async Task<FtpProfile> AutoConnect(CancellationToken token = default(CancellationToken)) {
			LogFunction(nameof(AutoConnect));

			// connect to the first available connection profile
			var results = await AutoDetect(new FtpAutoDetectConfig() {
				FirstOnly = true,
				CloneConnection = false,
			}, token);
			if (results.Count > 0) {
				var profile = results[0];

				// load the profile so final property selections are
				// loaded into the current connection
				LoadProfile(profile);

				// if we are using SSL, set a basic server acceptance function
				ConnectModule.SetDefaultCertificateValidation(this, profile);

				// return the working profile
				return profile;
			}

			return null;
		}

	}
}
