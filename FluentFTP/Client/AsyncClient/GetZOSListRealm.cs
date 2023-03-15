using FluentFTP.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// If an FTP Server has "different realms", in which realm is the
		/// current working directory. 
		/// </summary>
		/// <returns>The realm</returns>
		public async Task<FtpZOSListRealm> GetZOSListRealm(CancellationToken token = default(CancellationToken)) {
			LogFunction(nameof(GetZOSListRealm));

			// this case occurs immediately after connection and after the working dir has changed
			if (Status.LastWorkingDir == null) {
				await ReadCurrentWorkingDirectory(token);
			}

			if (ServerType != FtpServer.IBMzOSFTP) {
				return FtpZOSListRealm.Invalid;
			}

			// It is a unix like path (starts with /)
			if (Status.LastWorkingDir[0] != '\'') {
				return FtpZOSListRealm.Unix;
			}

			// Ok, the CWD starts with a single quote. Classic z/OS dataset realm
			FtpReply reply;

			// Fetch the current working directory. The reply will tell us what it is we are...
			if (!(reply = await Execute("CWD " + Status.LastWorkingDir, token)).Success) {
				throw new FtpCommandException(reply);
			}

			// 250-The working directory may be a load library                          
			// 250 The working directory "GEEK.PRODUCTS.LOADLIB" is a partitioned data set

			if (reply.InfoMessages?.Contains("may be a load library") == true) {
				return FtpZOSListRealm.MemberU;
			}

			if (reply.Message.Contains("is a partitioned data set")) {
				return FtpZOSListRealm.Member;
			}

			return FtpZOSListRealm.Dataset;
		}

	}
}
