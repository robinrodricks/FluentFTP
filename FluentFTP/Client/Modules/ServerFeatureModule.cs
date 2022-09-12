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
		public static void Detect(List<FtpCapability> capabilities, ref FtpHashAlgorithm hashAlgorithms, string[] features) {
			foreach (var feat in features) {
				var featName = feat.Trim().ToUpper();

				// Handle possible multiline FEAT reply format
				if (featName.StartsWith("211-")) {
					featName = featName.Substring(4);
				}

				if (featName.StartsWith("MLST") || featName.StartsWith("MLSD")) {
					capabilities.AddOnce(FtpCapability.MLSD);
				}
				else if (featName.StartsWith("MDTM")) {
					capabilities.AddOnce(FtpCapability.MDTM);
				}
				else if (featName.StartsWith("REST STREAM")) {
					capabilities.AddOnce(FtpCapability.REST);
				}
				else if (featName.StartsWith("SIZE")) {
					capabilities.AddOnce(FtpCapability.SIZE);
				}
				else if (featName.StartsWith("UTF8")) {
					capabilities.AddOnce(FtpCapability.UTF8);
				}
				else if (featName.StartsWith("PRET")) {
					capabilities.AddOnce(FtpCapability.PRET);
				}
				else if (featName.StartsWith("MFMT")) {
					capabilities.AddOnce(FtpCapability.MFMT);
				}
				else if (featName.StartsWith("MFCT")) {
					capabilities.AddOnce(FtpCapability.MFCT);
				}
				else if (featName.StartsWith("MFF")) {
					capabilities.AddOnce(FtpCapability.MFF);
				}
				else if (featName.StartsWith("MMD5")) {
					capabilities.AddOnce(FtpCapability.MMD5);
				}
				else if (featName.StartsWith("XMD5")) {
					capabilities.AddOnce(FtpCapability.XMD5);
				}
				else if (featName.StartsWith("XCRC")) {
					capabilities.AddOnce(FtpCapability.XCRC);
				}
				else if (featName.StartsWith("XSHA1")) {
					capabilities.AddOnce(FtpCapability.XSHA1);
				}
				else if (featName.StartsWith("XSHA256")) {
					capabilities.AddOnce(FtpCapability.XSHA256);
				}
				else if (featName.StartsWith("XSHA512")) {
					capabilities.AddOnce(FtpCapability.XSHA512);
				}
				else if (featName.StartsWith("EPSV")) {
					capabilities.AddOnce(FtpCapability.EPSV);
				}
				else if (featName.StartsWith("CPSV")) {
					capabilities.AddOnce(FtpCapability.CPSV);
				}
				else if (featName.StartsWith("NOOP")) {
					capabilities.AddOnce(FtpCapability.NOOP);
				}
				else if (featName.StartsWith("CLNT")) {
					capabilities.AddOnce(FtpCapability.CLNT);
				}
				else if (featName.StartsWith("SSCN")) {
					capabilities.AddOnce(FtpCapability.SSCN);
				}
				else if (featName.StartsWith("SITE MKDIR")) {
					capabilities.AddOnce(FtpCapability.SITE_MKDIR);
				}
				else if (featName.StartsWith("SITE RMDIR")) {
					capabilities.AddOnce(FtpCapability.SITE_RMDIR);
				}
				else if (featName.StartsWith("SITE UTIME")) {
					capabilities.AddOnce(FtpCapability.SITE_UTIME);
				}
				else if (featName.StartsWith("SITE SYMLINK")) {
					capabilities.AddOnce(FtpCapability.SITE_SYMLINK);
				}
				else if (featName.StartsWith("AVBL")) {
					capabilities.AddOnce(FtpCapability.AVBL);
				}
				else if (featName.StartsWith("THMB")) {
					capabilities.AddOnce(FtpCapability.THMB);
				}
				else if (featName.StartsWith("RMDA")) {
					capabilities.AddOnce(FtpCapability.RMDA);
				}
				else if (featName.StartsWith("DSIZ")) {
					capabilities.AddOnce(FtpCapability.DSIZ);
				}
				else if (featName.StartsWith("HOST")) {
					capabilities.AddOnce(FtpCapability.HOST);
				}
				else if (featName.StartsWith("CCC")) {
					capabilities.AddOnce(FtpCapability.CCC);
				}
				else if (featName.StartsWith("MODE Z")) {
					capabilities.AddOnce(FtpCapability.MODE_Z);
				}
				else if (featName.StartsWith("LANG")) {
					capabilities.AddOnce(FtpCapability.LANG);
				}
				else if (featName.StartsWith("HASH")) {
					Match m;

					capabilities.AddOnce(FtpCapability.HASH);

					if ((m = Regex.Match(featName, @"^HASH\s+(?<types>.*)$")).Success) {
						foreach (var type in m.Groups["types"].Value.Split(';')) {
							switch (type.ToUpper().Trim()) {
								case "SHA-1":
								case "SHA-1*":
									hashAlgorithms |= FtpHashAlgorithm.SHA1;
									break;

								case "SHA-256":
								case "SHA-256*":
									hashAlgorithms |= FtpHashAlgorithm.SHA256;
									break;

								case "SHA-512":
								case "SHA-512*":
									hashAlgorithms |= FtpHashAlgorithm.SHA512;
									break;

								case "MD5":
								case "MD5*":
									hashAlgorithms |= FtpHashAlgorithm.MD5;
									break;

								case "CRC":
								case "CRC*":
									hashAlgorithms |= FtpHashAlgorithm.CRC;
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
		public static void Assume(FtpBaseServer handler, List<FtpCapability> capabilities, ref FtpHashAlgorithm hashAlgorithms) {

			// ask the server handler to assume its capabilities
			if (handler != null) {
				var caps = handler.DefaultCapabilities();
				if (caps != null) {

					// add the assumed capabilities to our set
					Detect(capabilities, ref hashAlgorithms, caps);
				}
			}

		}

	}
}
