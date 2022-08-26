using System.Collections.Generic;
using System.Threading;
using FluentFTP.Client.Modules;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Load the given connection profile and configure the FTP client instance accordingly.
		/// </summary>
		/// <param name="profile">Connection profile. Not modified.</param>
		public void LoadProfile(FtpProfile profile) {
			ConnectModule.LoadProfile(this, profile);
		}


		#region Auto Detect

		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and returns the list of successful connection profiles.
		/// You can configure it to stop after finding the first successful profile, or to collect all successful profiles.
		/// You can then generate code for the profile using the FtpProfile.ToCode method.
		/// If no successful profiles are found, a blank list is returned.
		/// </summary>
		/// <param name="firstOnly">Find all successful profiles (false) or stop after finding the first successful profile (true)</param>
		/// <param name="cloneConnection">Use a new cloned FtpClient for testing connection profiles (true) or use the source FtpClient (false)</param>
		/// <returns></returns>
		public List<FtpProfile> AutoDetect(bool firstOnly = true, bool cloneConnection = true) {

			lock (m_lock) {
				LogFunc(nameof(AutoDetect), new object[] { firstOnly, cloneConnection });
				ValidateAutoDetect();

				return ConnectModule.AutoDetect(this, firstOnly, cloneConnection);
			}
		}

#if ASYNC
		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and returns the list of successful connection profiles.
		/// You can configure it to stop after finding the first successful profile, or to collect all successful profiles.
		/// You can then generate code for the profile using the FtpProfile.ToCode method.
		/// If no successful profiles are found, a blank list is returned.
		/// </summary>
		/// <param name="firstOnly">Find all successful profiles (false) or stop after finding the first successful profile (true)</param>
		/// <param name="cloneConnection">Use a new cloned FtpClient for testing connection profiles (true) or use the source FtpClient (false)</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns></returns>
		public async Task<List<FtpProfile>> AutoDetectAsync(bool firstOnly, bool cloneConnection = true, CancellationToken token = default(CancellationToken)) {
			var results = new List<FtpProfile>();

			LogFunc(nameof(AutoDetectAsync), new object[] { firstOnly, cloneConnection });
			ValidateAutoDetect();

			return await ConnectModule.AutoDetectAsync(this, firstOnly, cloneConnection, token);
		}
#endif

		#endregion

		#region Auto Connect

		/// <summary>
		/// Connect to the given server profile.
		/// </summary>
		public void Connect(FtpProfile profile) {

			// copy over the profile properties to this instance
			LoadProfile(profile);

			// begin connection
			Connect();
		}

#if ASYNC
		/// <summary>
		/// Connect to the given server profile.
		/// </summary>
		public async Task ConnectAsync(FtpProfile profile, CancellationToken token = default(CancellationToken)) {

			// copy over the profile properties to this instance
			LoadProfile(profile);

			// begin connection
			await ConnectAsync(token);
		}
#endif

		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and connects to the first successful profile.
		/// Returns the FtpProfile if the connection succeeded, or null if it failed.
		/// It will throw exceptions for permanent failures like invalid host or invalid credentials.
		/// </summary>
		public FtpProfile AutoConnect() {
			LogFunc(nameof(AutoConnect));

			// connect to the first available connection profile
			var results = AutoDetect(true, false);
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

#if ASYNC
		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and connects to the first successful profile.
		/// Returns the FtpProfile if the connection succeeded, or null if it failed.
		/// It will throw exceptions for permanent failures like invalid host or invalid credentials.
		/// </summary>
		public async Task<FtpProfile> AutoConnectAsync(CancellationToken token = default(CancellationToken)) {
			LogFunc(nameof(AutoConnectAsync));

			// connect to the first available connection profile
			var results = await AutoDetectAsync(true, false, token);
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
#endif

		#endregion
	}
}
