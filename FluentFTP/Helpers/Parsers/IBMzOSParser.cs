using System;
using System.Globalization;

#if NET45
using System.Threading.Tasks;

#endif

namespace FluentFTP.Helpers.Parsers {
	internal static class IBMzOSParser {
		/// <summary>
		/// Checks if the given listing is a valid IBM z/OS file listing
		/// </summary>
		public static bool IsValid(FtpClient client, string[] listing) {
			// Check validity by using the title line
			// USS Realm     : "total nnnn"
			// Dataset       : "Volume Unit    Referred Ext Used Recfm Lrecl BlkSz Dsorg Dsname"
			// Member        : " Name     VV.MM   Created       Changed      Size  Init   Mod   Id"
			// Member Loadlib: " Name      Size     TTR   Alias-of AC--------- Attributes--------- Amode Rmode"

			return listing[0].Contains("total") ||
				   listing[0].Contains("Volume Unit") ||
				   listing[0].Contains("Name     VV.MM") ||
				   listing[0].Contains("Name      Size     TTR");
		}

		/// <summary>
		/// Parses IBM z/OS format listings
		/// </summary>
		/// <param name="client">The FTP client</param>
		/// <param name="record">A line from the listing</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		public static FtpListItem Parse(FtpClient client, string record, string path) {
			// Skip title line - all modes have one. 
			// Also set zOSListingRealm to remember the mode we are in

			// "total nnnn"
			if (record.Contains("total")) {
				client.zOSListingRealm = FtpZOSListRealm.Unix;
				return null;
			}

			// "Volume Unit    Referred Ext Used Recfm Lrecl BlkSz Dsorg Dsname"
			if (record.Contains("Volume Unit")) {
				client.zOSListingRealm = FtpZOSListRealm.Dataset;
				return null;
			}

			// " Name     VV.MM   Created       Changed      Size  Init   Mod   Id"
			if (record.Contains("Name     VV.MM")) {
				// This is an opportunity to issue XDSS and get the LRECL, but how?
				FtpReply reply;
				string cwd;
				// Is caller using FtpListOption.NoPath and CWD to the right place?
				if (path.Length == 0) {
					cwd = client.GetWorkingDirectory();
				}
				// Caller is not using FtpListOption.NoPath, so the path can be used
				// but needs modification depending on its ending. Remove the "(...)"
				else if (path.EndsWith(")'")) {
					cwd = path.Substring(0, path.IndexOf('(')) + "\'";
				}
				else if (path.EndsWith(")")) {
					cwd = path.Substring(0, path.IndexOf('('));
				}
				else {
					cwd = path;
				}
				if (!(reply = client.Execute("XDSS " + cwd)).Success) {
					throw new FtpCommandException(reply);
				}
				// SITE PDSTYPE=PDSE RECFM=FB BLKSIZE=16000 DIRECTORY=1 LRECL=80 PRIMARY=3 SECONDARY=110 TRACKS EATTR=SYSTEM
				string[] words = reply.Message.Split(' ');
				string[] val = words[5].Split('=');
				client.zOSListingLRECL = UInt16.Parse(val[1]);
				client.zOSListingRealm = FtpZOSListRealm.Member;
				return null;
			}

			// "Name      Size     TTR   Alias-of AC--------- Attributes--------- Amode Rmode"
			if (record.Contains("Name      Size     TTR")) {
				client.zOSListingRealm = FtpZOSListRealm.MemberU;
				return null;
			}

			if (client.zOSListingRealm == FtpZOSListRealm.Unix) {
				// HFS (=unix) mode
				//
				//total 17904
				//drwxrwxr-x   2 OMVSKERN SYS1        8192 Oct 19  2015 downloads
				//drwxrwxr-x   4 OMVSKERN SYS1        8192 Oct 19  2015 p5zipfile
				//-rw-rw----   1 YNSAS    SYS1     2723828 Dec  6  2021 t.out
				//-rw-r-----   1 YNSAS    SYS1      132480 Jan  2  2022 test.bin
				//-rw-rw----   1 YNSAS    SYS1     6209406 May 29  2021 test.tst
				//-rw-rw----   1 YNSAS    SYS1       47227 Jun  7  2021 test.txt
				//
				return UnixParser.Parse(client, record);
			}

			if (client.zOSListingRealm == FtpZOSListRealm.Dataset) {
				// PS/PO mode
				//
				//Volume Unit    Referred Ext Used Recfm Lrecl BlkSz Dsorg Dsname    
				//ANSYBG 3390   2020/01/03  1   15  VB   32756 32760  PS  $.ADATA.XAA
				//ANSYBH 3390   2022/02/18  1+++++  VBS  32767 27966  PS  $.BDATA.XBB
				//

				// Ignore title line AND also ignore "VSAM", "Not Mounted" and "Error determining attributes"

				if (record.Substring(51, 4).Trim() == "PO" || record.Substring(51, 4).Trim() == "PS") {
					string volume = record.Substring(0, 6);
					string unit = record.Substring(7, 4);
					string referred = record.Substring(14, 10).Trim();
					string ext = record.Substring(25, 2).Trim();
					string used = record.Substring(27, 5).Trim();
					string recfm = record.Substring(34, 4).Trim();
					string lrecl = record.Substring(39, 5).Trim();
					string blksz = record.Substring(45, 5).Trim();
					string dsorg = record.Substring(51, 4).Trim();
					string dsname = record.Remove(0, 56).Trim().Split(' ')[0];
					bool isDir = dsorg == "PO";
					var lastModifiedStr = referred;
					if (lastModifiedStr != "**NONE**") {
						lastModifiedStr += " 00:00";
					}
					var lastModified = ParseDateTime(client, lastModifiedStr);
					// If "+++++" we could assume maximum "normal" size of 65535 tracks. (3.46GB)
					// or preferably "large format sequential" of 16777215 tracks (885.38GB)
					// This is a huge over-estimation in all probability but it cannot be helped.
					var size = 16777216L * 56664L;
					if (used != "+++++") {
						size = long.Parse(used) * 56664L; // 3390 dev bytes per track
					}
					var file = new FtpListItem(record, dsname, size, isDir, lastModified);
					return file;
				}
				return null;
			}

			if (client.zOSListingRealm == FtpZOSListRealm.Member) {
				// Member mode
				//
				// Name     VV.MM   Created       Changed      Size  Init   Mod   Id   
				//$2CPF1    01.01 2001/10/18 2001/10/18 11:58    29    29     0 QFX3076
				//

				string name = record.Substring(0, 8).Trim();
				string changed = string.Empty;
				string records = "0";
				// Member stats may be empty
				if (record.TrimEnd().Length > 8) {
					string vvmm = record.Substring(10, 5).Trim();
					string created = record.Substring(17, 10).Trim();
					changed = record.Substring(27, 16).Trim();
					records = record.Substring(44, 5).Trim();
					string init = record.Substring(50, 5).Trim();
					string mod = record.Substring(56, 5).Trim();
					string id = record.Substring(62, 6).Trim();
				}
				bool isDir = false;
				var lastModifiedStr = changed;
				var lastModified = ParseDateTime(client, lastModifiedStr);
				var size = ushort.Parse(records) * client.zOSListingLRECL;
				var file = new FtpListItem(record, name, size, isDir, lastModified);
				return file;
			}

			if (client.zOSListingRealm == FtpZOSListRealm.MemberU) {
				// Member Loadlib mode
				//
				// Name      Size     TTR   Alias-of AC --------- Attributes --------- Amode Rmode
				//EAGKCPT   000058   000009          00 FO             RN RU            31    ANY
				//EAGRTPRC  005F48   000011 EAGRTALT 00 FO             RN RU            31    ANY
				//

				string name = record.Substring(0, 8).Trim();
				string changed = string.Empty;
				string memsize = record.Substring(10, 6);
				string TTR = record.Substring(19, 6);
				string Alias = record.Substring(26, 8).Trim();
				string Attributes = record.Substring(38, 30);
				string Amode = record.Substring(70, 2);
				string Rmode = record.Substring(76, 3);
				bool isDir = false;
				var lastModifiedStr = changed;
				var lastModified = ParseDateTime(client, lastModifiedStr);
				var size = int.Parse(memsize, System.Globalization.NumberStyles.HexNumber);
				var file = new FtpListItem(record, name, size, isDir, lastModified);
				return file;
			}

			return null;
		}

		/// <summary>
		/// Parses the last modified date from IBM z/OS format listings
		/// </summary>
		private static DateTime ParseDateTime(FtpClient client, string lastModifiedStr) {
			var lastModified = DateTime.MinValue;
			if (lastModifiedStr == string.Empty || lastModifiedStr == "**NONE**") {
				return lastModified;
			}
			lastModified = DateTime.ParseExact(lastModifiedStr, @"yyyy'/'MM'/'dd HH':'mm", client.ListingCulture.DateTimeFormat, DateTimeStyles.None);

			return lastModified;
		}
	}
}