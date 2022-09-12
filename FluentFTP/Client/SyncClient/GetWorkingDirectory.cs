using System;
using System.Linq;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Gets the current working directory
		/// </summary>
		/// <returns>The current working directory, ./ if the response couldn't be parsed.</returns>
		public string GetWorkingDirectory() {
			return ((IInternalFtpClient)this).GetWorkingDirectoryInternal();
		}
	}
}
