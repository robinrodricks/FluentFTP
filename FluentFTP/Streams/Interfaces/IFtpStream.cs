using FluentFTP.Client.BaseClient;
using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;

namespace FluentFTP.Streams {
	public interface IFtpStream {

		void Init(
			BaseFtpClient client,
			string targetHost,
			Socket socket,
			CustomRemoteCertificateValidationCallback customRemoteCertificateValidation,
			bool isControl,
			IFtpStream controlConnStream,
			IFtpStreamConfig config);

		Stream GetBaseStream();

		bool CanRead();
		bool CanWrite();

		SslProtocols GetSslProtocol();
		string GetCipherSuite(); 

		void Dispose();


	}
}
