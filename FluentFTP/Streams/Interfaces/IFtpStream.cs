using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP.Streams {
	public interface IFtpStream {

		Stream GetBaseStream();
		bool CanRead();
		bool CanWrite();
		SslProtocols GetSslProtocol();

		string GetCipherSuite(); 
		void Dispose();


	}
}
