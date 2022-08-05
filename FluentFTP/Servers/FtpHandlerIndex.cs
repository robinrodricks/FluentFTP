using System.Collections.Generic;
using FluentFTP.Servers.Handlers;

namespace FluentFTP.Servers {
	internal static class FtpHandlerIndex {

		public static List<FtpBaseServer> AllServers = new List<FtpBaseServer> {
			new BFtpdServer(),
			new CerberusServer(),
			new CrushFtpServer(),
			new FileZillaServer(),
			new FritzBoxServer(),
			new Ftp2S3GatewayServer(),
			new GlFtpdServer(),
			new GlobalScapeEftServer(),
			new HomegateFtpServer(),
			new IBMOS400FtpServer(),
			new IBMzOSFtpServer(),
			new NonStopTandemServer(),
			new OpenVmsServer(),
			new ProFtpdServer(),
			new PureFtpdServer(),
			new PyFtpdLibServer(),
			new ServUServer(),
			new SolarisFtpServer(),
			new VsFtpdServer(),
			new WindowsCEServer(),
			new WindowsIISServer(),
			new WSFTPServer(),
			new WuFtpdServer(),
			new XLightServer(),
			new TitanFtpServer(),
			new RumpusServer(),
			new IDALFtpServer(),
		};


	}
}
