using FluentFTP.Client.Modules;
using System.Collections.Generic;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Populates the capabilities flags based on capabilities
		/// supported by this server. This method is overridable
		/// so that new features can be supported
		/// </summary>
		/// <param name="reply">The reply object from the FEAT command. The InfoMessages property will
		/// contain a list of the features the server supported delimited by a new line '\n' character.</param>
		protected virtual void GetFeatures(FtpReply reply) {
			ServerFeatureModule.Detect(m_capabilities, ref m_hashAlgorithms, reply.InfoMessages.Split('\n'));
		}

		/// <summary>
		/// Forcibly set the capabilities of your FTP server.
		/// By default capabilities are loaded automatically after calling Connect and you don't need to use this method.
		/// This is only for advanced use-cases.
		/// </summary>
		public void SetFeatures(List<FtpCapability> capabilities) {
			m_capabilities = capabilities;
		}

		/// <summary>
		/// Performs a bitwise and to check if the specified
		/// flag is set on the <see cref="Capabilities"/>  property.
		/// </summary>
		/// <param name="cap">The <see cref="FtpCapability"/> to check for</param>
		/// <returns>True if the feature was found, false otherwise</returns>
		public bool HasFeature(FtpCapability cap) {
			if (cap == FtpCapability.NONE && Capabilities.Count == 0) {
				return true;
			}

			return Capabilities.Contains(cap);
		}
	}
}
