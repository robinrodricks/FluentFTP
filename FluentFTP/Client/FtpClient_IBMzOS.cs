using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Helpers;
using FluentFTP.Proxy;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;
using FluentFTP.Helpers.Hashing;
using HashAlgos = FluentFTP.Helpers.Hashing.HashAlgorithms;

#endif
#if ASYNC
using System.Threading.Tasks;

#endif

namespace FluentFTP {
	public partial class FtpClient : IDisposable {

		#region Get z/OS Realm

		/// <summary>
		/// If an FTP Server has "different realms", in which realm is the
		/// current working directory. 
		/// </summary>
		/// <returns>The realm</returns>
		public FtpzOSListRealm GetzOSListRealm() {

			// this case occurs immediately after connection and after the working dir has changed
			if (_LastWorkingDir == null) {
				ReadCurrentWorkingDirectory();
			}

			if (ServerType != FtpServer.IBMzOSFTP ||
				ServerOS != FtpOperatingSystem.IBMzOS) {
				return FtpzOSListRealm.Invalid;
			}

			// It is a unix like path (starts with /)
			if (_LastWorkingDir[0] != '\'') {
				return FtpzOSListRealm.Unix;
			}

			// Ok, the CWD starts with a single quoute. Classic z/OS dataset realm
			FtpReply reply;

#if !CORE14
			lock (m_lock) {
#endif
				// Go to where we are. The reply will tell us what it is we we are...
				if (!(reply = Execute("CWD " + _LastWorkingDir)).Success) {
					throw new FtpCommandException(reply);
				}
#if !CORE14
			}
#endif
			// 250-The working directory may be a load library                          
			// 250 The working directory "GEEK.PRODUCT.LOADLIB" is a partitioned data set

			if (reply.InfoMessages != null &&
				reply.InfoMessages.Contains("may be a load library")) {
				return FtpzOSListRealm.MemberU;
			}

			if (reply.Message.Contains("is a partitioned data set")) {
				return FtpzOSListRealm.Member;
			}

			return FtpzOSListRealm.Dataset;
		}

#if ASYNC
		/// <summary>
		/// If an FTP Server has "different realms", in which realm is the
		/// current working directory. 
		/// </summary>
		/// <returns>The realm</returns>
		public async Task<FtpzOSListRealm> GetzOSListRealmAsync(CancellationToken token = default(CancellationToken))
		{

			// this case occurs immediately after connection and after the working dir has changed
			if (_LastWorkingDir == null)
			{
				await ReadCurrentWorkingDirectoryAsync(token);
			}

			if (ServerType != FtpServer.IBMzOSFTP ||
				ServerOS != FtpOperatingSystem.IBMzOS)			{
				return FtpzOSListRealm.Invalid;
			}

			// It is a unix like path (starts with /)
			if (_LastWorkingDir[0] != '\'')
			{
				return FtpzOSListRealm.Unix;
			}

			// Ok, the CWD starts with a single quoute. Classic z/OS dataset realm
			FtpReply reply;

			// Go to where we are. The reply will tell us what it is we we are...
			if (!(reply = await Execute("CWD " + _LastWorkingDir, token)).Success)
			{
				throw new FtpCommandException(reply);
			}

			// 250-The working directory may be a load library                          
			// 250 The working directory "GEEK.PRODUCTS.LOADLIB" is a partitioned data set

			if (reply.InfoMessages!=null &&
				reply.InfoMessages.Contains("may be a load library"))
			{
				return FtpzOSListRealm.MemberU;
			}

			if (reply.Message.Contains("is a partitioned data set"))
			{
				return FtpzOSListRealm.Member;
			}

			return FtpzOSListRealm.Dataset;
		}
#endif
		#endregion

		#region Get z/OS File Size

		/// <summary>
		/// Get z/OS file size
		/// </summary>
		/// <returns>The size of the file</returns>
		public long GetzOSFileSize(string path) {
			string oldPath = "";
			string cwdPath = path;
			string[] preFix = path.Split('(');
			if (preFix.Length > 1) // PDS Member
			{
				cwdPath = preFix[0] + '\'';
				oldPath = GetWorkingDirectory();
				SetWorkingDirectory(cwdPath);
			}
			ListingParser = FtpParser.IBMzOS;
			FtpListItem[] entries = GetListing(path);
			if (entries.Length != 1) return -1;
			FtpListItem entry = entries[0];

			if (preFix.Length > 1) {
				SetWorkingDirectory(oldPath);
			}
			return entry.Size;
		}

#if ASYNC
		/// <summary>
		/// Get z/OS file size
		/// </summary>
		/// <returns>The size of the file</returns>
		public async Task<long> GetzOSFileSizeAsync(string path, CancellationToken token = default(CancellationToken))
		{
			string oldPath = "";     
			string cwdPath = path;
			string[] preFix = path.Split('(');
			if (preFix.Length > 1) // PDS Member
			{
				cwdPath = preFix[0] + '\'';
				oldPath = await GetWorkingDirectory(token);
				await SetWorkingDirectoryAsync(cwdPath, token);
			}
			ListingParser = FtpParser.IBMzOS;
			FtpListItem[] entries = await GetListingAsync(path, token);
			if (entries.Length != 1) return -1;
			FtpListItem entry = entries[0];

			if (preFix.Length > 1)
			{
				await SetWorkingDirectoryAsync(oldPath, token);
			}
			return entry.Size;

		}
#endif


		#endregion

	}
}
