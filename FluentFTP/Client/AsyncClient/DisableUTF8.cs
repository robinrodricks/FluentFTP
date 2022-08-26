using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using FluentFTP.Exceptions;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using FluentFTP.Client.Modules;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Disables UTF8 support and changes the Encoding property
		/// back to ASCII. If the server returns an error when trying
		/// to turn UTF8 off a FtpCommandException will be thrown.
		/// </summary>
		public void DisableUTF8() {
			FtpReply reply;

			lock (m_lock) {
				if (!(reply = Execute("OPTS UTF8 OFF")).Success) {
					throw new FtpCommandException(reply);
				}

				m_textEncoding = Encoding.ASCII;
				m_textEncodingAutoUTF = false;
			}

		}
	}
}
