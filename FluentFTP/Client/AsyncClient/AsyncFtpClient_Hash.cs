using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Helpers;
using FluentFTP.Proxy;
#if !NETSTANDARD
using System.Web;
#endif
#if NETSTANDARD
using System.Threading;

#endif
#if ASYNC
using System.Threading.Tasks;

#endif
using FluentFTP.Helpers.Hashing;
using HashAlgos = FluentFTP.Helpers.Hashing.HashAlgorithms;

namespace FluentFTP {
	public partial class AsyncFtpClient {

	}
}