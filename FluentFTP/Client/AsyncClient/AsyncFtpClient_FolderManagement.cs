using System;
using System.Text.RegularExpressions;
using System.Linq;
using FluentFTP.Helpers;
using FluentFTP.Client;
#if !NETSTANDARD
using System.Web;
using FluentFTP.Client.Modules;
#endif
#if NETSTANDARD
using System.Threading;
using FluentFTP.Client.Modules;
using FluentFTP.Client;

#endif
#if ASYNC
using System.Threading.Tasks;

#endif

namespace FluentFTP {
	public partial class AsyncFtpClient {

	}
}