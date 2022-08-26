using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Exceptions;
using FluentFTP.Proxy;
using FluentFTP.Helpers;
#if !NETSTANDARD
using System.Web;
#endif
#if NETSTANDARD
using System.Threading;
using System.Runtime.CompilerServices;
#endif
#if ASYNC
using System.Threading.Tasks;
#endif
using FluentFTP.Client.Modules;

namespace FluentFTP {
	public partial class AsyncFtpClient {

	}
}