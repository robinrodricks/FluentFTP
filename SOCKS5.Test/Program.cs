using FluentFTP.Proxy;
using System;

namespace SOCKS5.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
			var ftp_host = "";
			var ftp_port = 0;
			var ftp_user = "";
			var ftp_pass = "";

			var socks5_host = "";
			var socks5_port = 0;
			var socks5_user = "";
			var socks5_pass = "";


			var client = new FtpClientSocks5Proxy(new ProxyInfo()
			{
				Host = socks5_host,
				Port = socks5_port,
				Credentials = new System.Net.NetworkCredential()
				{
					UserName = socks5_user,
					Password = socks5_pass
				}
			});

			client.Host = ftp_host;
			client.Port = ftp_port;
			client.Credentials = new System.Net.NetworkCredential()
			{
				UserName = ftp_user,
				Password = ftp_pass
			};

			client.Connect();

			var listing = client.GetListing("/");



        }
    }
}
