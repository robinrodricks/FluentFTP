using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP {
	internal interface IInternalFtpClient {

		string GetWorkingDirectoryInternal();

		FtpReply ExecuteInternal(string command);

		void DisconnectInternal();

		void ConnectInternal();

		FtpReply CloseDataStreamInternal(FtpDataStream stream);

		void LogStatus(FtpTraceLevel eventType, string message);

		void LogLine(FtpTraceLevel eventType, string message);

		FtpSocketStream GetBaseStream();

		void SetListingParser(FtpParser parser);

	}
}