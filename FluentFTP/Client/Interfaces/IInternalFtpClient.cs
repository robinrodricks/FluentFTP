using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	internal interface IInternalFtpClient {

		void ConnectInternal(bool reConnect);
		void ConnectInternal(bool reConnect, CancellationToken token);

		void DisconnectInternal();
		void DisconnectInternal(CancellationToken token);

		FtpReply ExecuteInternal(string command);

		string GetWorkingDirectoryInternal();

		FtpReply CloseDataStreamInternal(FtpDataStream stream);

		void LogStatus(FtpTraceLevel eventType, string message);

		void LogLine(FtpTraceLevel eventType, string message);

		FtpSocketStream GetBaseStream();

		void SetListingParser(FtpParser parser);

	}
}