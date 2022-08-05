using FluentFTP.Helpers;
using FluentFTP.Servers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FluentFTP.Client.Modules {
	internal static class ServerFeatureModule {

		/// <summary>
		/// Populates the capabilities flags based on capabilities given in the list of strings.
		/// </summary>
		public static void Detect(List<FtpCapability> m_capabilities, ref FtpHashAlgorithm m_hashAlgorithms, string[] features) {
			foreach (var feat in features) {
				var featName = feat.Trim().ToUpper();

				// Handle possible multiline FEAT reply format
				if (featName.StartsWith("211-")) {
					featName = featName.Substring(4);
				}

				if (featName.StartsWith("MLST") || featName.StartsWith("MLSD")) {
					m_capabilities.AddOnce(FtpCapability.MLSD);
				}
				else if (featName.StartsWith("MDTM")) {
					m_capabilities.AddOnce(FtpCapability.MDTM);
				}
				else if (featName.StartsWith("REST STREAM")) {
					m_capabilities.AddOnce(FtpCapability.REST);
				}
				else if (featName.StartsWith("SIZE")) {
					m_capabilities.AddOnce(FtpCapability.SIZE);
				}
				else if (featName.StartsWith("UTF8")) {
					m_capabilities.AddOnce(FtpCapability.UTF8);
				}
				else if (featName.StartsWith("PRET")) {
					m_capabilities.AddOnce(FtpCapability.PRET);
				}
				else if (featName.StartsWith("MFMT")) {
					m_capabilities.AddOnce(FtpCapability.MFMT);
				}
				else if (featName.StartsWith("MFCT")) {
					m_capabilities.AddOnce(FtpCapability.MFCT);
				}
				else if (featName.StartsWith("MFF")) {
					m_capabilities.AddOnce(FtpCapability.MFF);
				}
				else if (featName.StartsWith("MMD5")) {
					m_capabilities.AddOnce(FtpCapability.MMD5);
				}
				else if (featName.StartsWith("XMD5")) {
					m_capabilities.AddOnce(FtpCapability.XMD5);
				}
				else if (featName.StartsWith("XCRC")) {
					m_capabilities.AddOnce(FtpCapability.XCRC);
				}
				else if (featName.StartsWith("XSHA1")) {
					m_capabilities.AddOnce(FtpCapability.XSHA1);
				}
				else if (featName.StartsWith("XSHA256")) {
					m_capabilities.AddOnce(FtpCapability.XSHA256);
				}
				else if (featName.StartsWith("XSHA512")) {
					m_capabilities.AddOnce(FtpCapability.XSHA512);
				}
				else if (featName.StartsWith("EPSV")) {
					m_capabilities.AddOnce(FtpCapability.EPSV);
				}
				else if (featName.StartsWith("CPSV")) {
					m_capabilities.AddOnce(FtpCapability.CPSV);
				}
				else if (featName.StartsWith("NOOP")) {
					m_capabilities.AddOnce(FtpCapability.NOOP);
				}
				else if (featName.StartsWith("CLNT")) {
					m_capabilities.AddOnce(FtpCapability.CLNT);
				}
				else if (featName.StartsWith("SSCN")) {
					m_capabilities.AddOnce(FtpCapability.SSCN);
				}
				else if (featName.StartsWith("SITE MKDIR")) {
					m_capabilities.AddOnce(FtpCapability.SITE_MKDIR);
				}
				else if (featName.StartsWith("SITE RMDIR")) {
					m_capabilities.AddOnce(FtpCapability.SITE_RMDIR);
				}
				else if (featName.StartsWith("SITE UTIME")) {
					m_capabilities.AddOnce(FtpCapability.SITE_UTIME);
				}
				else if (featName.StartsWith("SITE SYMLINK")) {
					m_capabilities.AddOnce(FtpCapability.SITE_SYMLINK);
				}
				else if (featName.StartsWith("AVBL")) {
					m_capabilities.AddOnce(FtpCapability.AVBL);
				}
				else if (featName.StartsWith("THMB")) {
					m_capabilities.AddOnce(FtpCapability.THMB);
				}
				else if (featName.StartsWith("RMDA")) {
					m_capabilities.AddOnce(FtpCapability.RMDA);
				}
				else if (featName.StartsWith("DSIZ")) {
					m_capabilities.AddOnce(FtpCapability.DSIZ);
				}
				else if (featName.StartsWith("HOST")) {
					m_capabilities.AddOnce(FtpCapability.HOST);
				}
				else if (featName.StartsWith("CCC")) {
					m_capabilities.AddOnce(FtpCapability.CCC);
				}
				else if (featName.StartsWith("MODE Z")) {
					m_capabilities.AddOnce(FtpCapability.MODE_Z);
				}
				else if (featName.StartsWith("LANG")) {
					m_capabilities.AddOnce(FtpCapability.LANG);
				}
				else if (featName.StartsWith("HASH")) {
					Match m;

					m_capabilities.AddOnce(FtpCapability.HASH);

					if ((m = Regex.Match(featName, @"^HASH\s+(?<types>.*)$")).Success) {
						foreach (var type in m.Groups["types"].Value.Split(';')) {
							switch (type.ToUpper().Trim()) {
								case "SHA-1":
								case "SHA-1*":
									m_hashAlgorithms |= FtpHashAlgorithm.SHA1;
									break;

								case "SHA-256":
								case "SHA-256*":
									m_hashAlgorithms |= FtpHashAlgorithm.SHA256;
									break;

								case "SHA-512":
								case "SHA-512*":
									m_hashAlgorithms |= FtpHashAlgorithm.SHA512;
									break;

								case "MD5":
								case "MD5*":
									m_hashAlgorithms |= FtpHashAlgorithm.MD5;
									break;

								case "CRC":
								case "CRC*":
									m_hashAlgorithms |= FtpHashAlgorithm.CRC;
									break;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Assume the FTP Server's capabilities if it does not support the FEAT command.
		/// </summary>
		public static void Assume(FtpBaseServer handler, List<FtpCapability> m_capabilities, ref FtpHashAlgorithm m_hashAlgorithms) {

			// ask the server handler to assume its capabilities
			if (handler != null) {
				var caps = handler.DefaultCapabilities();
				if (caps != null) {

					// add the assumed capabilities to our set
					Detect(m_capabilities, ref m_hashAlgorithms, caps);
				}
			}

		}

	}
}
