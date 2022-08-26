using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP {
	internal interface IInternalFtpClient {

		string GetWorkingDirectoryInternal();
		
		FtpReply ExecuteInternal(string command);

		void DisconnectInternal();

		void ConnectInternal();

	}
}
