using System;
#if !NETSTANDARD
using System.Web;
using FluentFTP.Client;
#endif
#if NETSTANDARD
using System.Threading;
using FluentFTP.Client;
#endif
#if ASYNC
using System.Threading.Tasks;

#endif
using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class AsyncFtpClient {

	}
}