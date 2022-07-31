using FluentFTP;
using FluentFTPServer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTPServer.Servers {
	internal static class FtpDockerIndex {

		public static List<FtpDocker> Servers = new List<FtpDocker>() {

			new FtpDocker() {
				Type = FtpServer.VsFTPd,
				DockerFolder = "vsftpd",
				DockerName = "vsftpd:fluentftp",
				RunCommand = "docker run --rm -it -p 21:21 -p 4559-4564:4559-4564 -e FTP_USER=fluentroot -e FTP_PASSWORD=fluentpass vsftpd:fluentftp",
				FtpUser = "fluentroot",
				FtpPass = "fluentpass",
			},
			new FtpDocker() {
				Type = FtpServer.CrushFTP,
				DockerFolder = "crushftp",
				DockerName = "crushftp:fluentftp",
				RunCommand = "docker run -p 21:21 -p 443:443 -p 2000-2100:2000-2100 -p 2222:2222 -p 8080:8080 -p 9090:9090 crushftp:fluentftp",
				FtpUser = "crushadmin",
				FtpPass = "crushadmin",
			},
			new FtpDocker() {
				Type = FtpServer.FileZilla,
				DockerFolder = "filezilla",
				DockerName = "filezilla:fluentftp",
				RunCommand = "docker run -d --name=filezilla -p 5800:5800 -v /docker/appdata/filezilla:/config:rw -v $HOME:/storage:rw filezilla:fluentftp",
				FtpUser = "filezilla",
				FtpPass = "filezilla",
			},
			new FtpDocker() {
				Type = FtpServer.glFTPd,
				DockerFolder = "filezilla",
				DockerName = "glftpd:fluentftp",
				RunCommand = "docker run --name=glFTPd --net=host -e GL_PORT=21 -e GL_RESET_ARGS=<arguments> glftpd:fluentftp",
				FtpUser = "glftpd",
				FtpPass = "glftpd",
			},
			new FtpDocker() {
				Type = FtpServer.ProFTPD,
				DockerFolder = "proftpd",
				DockerName = "proftpd:fluentftp",
				RunCommand = "docker run -d --net host -e FTP_LIST=\"fluentroot:fluentpass\" -e MASQUERADE_ADDRESS=1.2.3.4 proftpd:fluentftp",
				FtpUser = "fluentroot",
				FtpPass = "fluentpass",
			},
			new FtpDocker() {
				Type = FtpServer.PureFTPd,
				DockerFolder = "pureftpd",
				DockerName = "pureftpd:fluentftp",
				RunCommand = "docker run -d --name ftpd_server -p 21:21 -p 30000-30009:30000-30009 -e \"PUBLICHOST=localhost\" -e \"FTP_USER_NAME=fluentroot\" -e \"FTP_USER_PASS=fluentpass\" pureftpd:fluentftp",
				FtpUser = "fluentroot",
				FtpPass = "fluentpass",
			},
			new FtpDocker() {
				Type = FtpServer.PyFtpdLib,
				DockerFolder = "pyftpdlib",
				DockerName = "pyftpdlib:fluentftp",
				RunCommand = "docker run -it --rm -p 21:21 pyftpdlib:fluentftp",
				FtpUser = "user",
				FtpPass = "password",
			},

		};
	}
}