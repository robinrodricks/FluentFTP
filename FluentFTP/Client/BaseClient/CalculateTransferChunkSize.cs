using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Proxy;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Client.Modules;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Calculate transfer chunk size taking rate control into account
		/// </summary>
		protected int CalculateTransferChunkSize(Int64 rateLimitBytes, int rateControlResolution) {
			int chunkSize = TransferChunkSize;

			// if user has not specified a TransferChunkSize and rate limiting is enabled
			if (m_transferChunkSize == null && rateLimitBytes > 0) {

				// reduce chunk size to optimize rate control
				const int chunkSizeMin = 64;
				while (chunkSize > chunkSizeMin) {
					var chunkLenInMs = 1000L * chunkSize / rateLimitBytes;
					if (chunkLenInMs <= rateControlResolution) {
						break;
					}

					chunkSize = Math.Max(chunkSize >> 1, chunkSizeMin);
				}
			}
			return chunkSize;
		}
	}
}
