using FluentFTP.Xunit.Docker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FluentFTP.Tests
{
	[CollectionDefinition("DockerCollection")]
	public class DockerCollection : ICollectionFixture<DockerFtpServerFixture>
	{
	}
}
