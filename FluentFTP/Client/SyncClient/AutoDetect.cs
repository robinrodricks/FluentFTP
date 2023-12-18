using System.Collections.Generic;
using System.Threading;
using FluentFTP.Client.Modules;
using System.Threading.Tasks;
using FluentFTP.Model.Functions;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and returns the list of successful connection profiles.
		/// You can configure it to stop after finding the first successful profile, or to collect all successful profiles.
		/// You can then generate code for the profile using the FtpProfile.ToCode method.
		/// If no successful profiles are found, a blank list is returned.
		/// </summary>
		/// <param name="config">The coresponding config object for this API</param>
		/// <returns></returns>
		public List<FtpProfile> AutoDetect(FtpAutoDetectConfig config = null) {

			LogFunction(nameof(AutoDetect), config);

			if (config == null) {
				config = new FtpAutoDetectConfig();
			}

			ValidateAutoDetect();

			return ConnectModule.AutoDetect(this, config);
		}

		/// <summary>
		/// LEGACY CALL FORMAT, to be deleted sometime in the future
		/// 
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

			LogFunction(nameof(AutoDetect), new object[] { firstOnly, cloneConnection });
			ValidateAutoDetect();

			return ConnectModule.AutoDetect(this, new FtpAutoDetectConfig() {
				FirstOnly = firstOnly,
				CloneConnection = cloneConnection,
			});
		}
	}
}
