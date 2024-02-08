using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {

	/// <summary>
	/// Interface for the InternalFtpClient class.
	/// For detailed documentation of the methods, please see the FtpClient class or check the Wiki on the FluentFTP Github project.
	/// </summary>
	public interface IInternalFtpClient {

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		void ConnectInternal(bool reConnect);
		Task ConnectInternal(bool reConnect, CancellationToken token);

		void DisconnectInternal();
		Task DisconnectInternal(CancellationToken token);

		void DisposeInternal();
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
		ValueTask DisposeInternal(CancellationToken token);
#else
		Task DisposeInternal(CancellationToken token);
#endif

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

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

	}
}