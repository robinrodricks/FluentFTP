using FluentFTP.Client.BaseClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP.Streams {
	public interface IFtpStream {

		void Init(BaseFtpClient client, Socket socket, bool isControl, IFtpStream controlConnStream, IFtpStreamConfig config);

		Stream GetBaseStream();
		bool CanRead();
		bool CanWrite();
		SslProtocols GetSslProtocol();

		string GetCipherSuite(); 
		void Dispose();


	}
}
