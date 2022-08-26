using System;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentFTP.Streams;
using FluentFTP.Helpers;
#if !NETSTANDARD
using System.Web;
#endif
#if NETSTANDARD
using FluentFTP.Exceptions;

#endif
#if NETSTANDARD
using System.Threading.Tasks;

#endif
using System.Threading;

namespace FluentFTP {
	public partial class AsyncFtpClient {

	}
}