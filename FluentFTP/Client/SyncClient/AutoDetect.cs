using System.Collections.Generic;
using System.Threading;
using FluentFTP.Client.Modules;
using System.Threading.Tasks;

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
		public List<FtpProfile> AutoDetect(FtpAutoDetectConfig config) {

			lock (m_lock) {
				// LogFunction(nameof(AutoDetect), new object[] { firstOnly, cloneConnection });

				ValidateAutoDetect();

				return ConnectModule.AutoDetect(this, config);
			}
		}

	}
}
