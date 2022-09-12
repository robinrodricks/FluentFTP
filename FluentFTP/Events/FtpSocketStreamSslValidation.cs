using System;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace FluentFTP {

	/// <summary>
	/// Event fired if a bad SSL certificate is encountered. This even is used internally; if you
	/// don't have a specific reason for using it you are probably looking for FtpSslValidation.
	/// </summary>
	/// <param name="stream"></param>
	/// <param name="e"></param>
	public delegate void FtpSocketStreamSslValidation(FtpSocketStream stream, FtpSslValidationEventArgs e);

}