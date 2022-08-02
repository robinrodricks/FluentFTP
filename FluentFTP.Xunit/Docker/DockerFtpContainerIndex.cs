using FluentFTP.Xunit.Docker.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP.Xunit.Docker {
	internal static class DockerFtpContainerIndex {
		
		public static List<DockerFtpContainer> Index = new List<DockerFtpContainer> {
			new CrushFtpContainer(),
			new FileZillaContainer(),
			new GlFtpdContainer(),
			new ProFtpdContainer(),
			new PureFtpdContainer(),
			new PyFtpdLibContainer(),
			new VsFtpdContainer()
		};
	}
}
