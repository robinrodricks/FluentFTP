using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public interface IInternalFtpClient {

		void ConnectInternal(bool reConnect);
		Task ConnectInternal(bool reConnect, CancellationToken token);

		void DisconnectInternal();
		Task DisconnectInternal(CancellationToken token);

		FtpReply ExecuteInternal(string command);

		FtpReply GetReplyInternal();
		FtpReply GetReplyInternal(string command);
		FtpReply GetReplyInternal(string command, bool exhaustNoop);
		FtpReply GetReplyInternal(string command, bool exhaustNoop, int timeOut);
		FtpReply GetReplyInternal(string command, bool exhaustNoop, int timeOut, bool useSema);

		Task<FtpReply> GetReplyInternal(CancellationToken token);
		Task<FtpReply> GetReplyInternal(CancellationToken token, string command);
		Task<FtpReply> GetReplyInternal(CancellationToken token, string command, bool exhaustNoop);
		Task<FtpReply> GetReplyInternal(CancellationToken token, string command, bool exhaustNoop, int timeOut);
		Task<FtpReply> GetReplyInternal(CancellationToken token, string command, bool exhaustNoop, int timeOut, bool useSema);

		bool IsStillConnectedInternal(int timeout = 10000);

		bool NoopInternal(bool ignoreNoopInterval = false);

		string GetWorkingDirectoryInternal();

		FtpReply CloseDataStreamInternal(FtpDataStream stream);
		Task<FtpReply> CloseDataStreamInternal(FtpDataStream stream, CancellationToken token);

		void LogStatus(FtpTraceLevel eventType, string message, Exception exception = null, bool exNewLine = false);

		void LogLine(FtpTraceLevel eventType, string message);

		FtpSocketStream GetBaseStream();

		void SetListingParser(FtpParser parser);
	}
}