using System;
using FluentFTP.Helpers;
#if !NETSTANDARD
using System.Web;
#endif
#if NETSTANDARD
using System.Threading;
using FluentFTP.Client.Modules;
#endif
#if ASYNC
using System.Threading.Tasks;
#endif
using System.Linq;
using FluentFTP.Client.Modules;
using FluentFTP.Client;

namespace FluentFTP {
	public partial class AsyncFtpClient {

	}
}