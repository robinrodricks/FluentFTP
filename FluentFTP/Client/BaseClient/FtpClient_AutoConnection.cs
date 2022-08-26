using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using FluentFTP.Proxy;
using SysSslProtocols = System.Security.Authentication.SslProtocols;
using FluentFTP.Servers;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Client.Modules;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		protected void ValidateAutoDetect() {
			if (IsDisposed) {
				throw new ObjectDisposedException("This FtpClient object has been disposed. It is no longer accessible.");
			}

			if (Host == null) {
				throw new FtpException("No host has been specified. Please set the 'Host' property before trying to auto connect.");
			}

			if (Credentials == null) {
				throw new FtpException("No username and password has been specified. Please set the 'Credentials' property before trying to auto connect.");
			}
		}

	}
}
