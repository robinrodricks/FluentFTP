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
		/// <param name="firstOnly">Find all successful profiles (false) or stop after finding the first successful profile (true)</param>
		/// <param name="cloneConnection">Use a new cloned FtpClient for testing connection profiles (true) or use the source FtpClient (false)</param>
		/// <returns></returns>
		public List<FtpProfile> AutoDetect(bool firstOnly = true, bool cloneConnection = true, bool requireEncryption = false, bool includeImplicit = true) {

			lock (m_lock) {
				LogFunction(nameof(AutoDetect), new object[] { firstOnly, cloneConnection, requireEncryption, includeImplicit });
				ValidateAutoDetect();

				return ConnectModule.AutoDetect(this, firstOnly, cloneConnection, requireEncryption, includeImplicit);
			}
		}

	}
}
