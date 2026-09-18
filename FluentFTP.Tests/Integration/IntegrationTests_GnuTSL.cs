using FluentFTP.Tests.Integration.System;

namespace FluentFTP.Tests.Integration {

	public class IntegrationTests_GnuTLS : IntegrationTestsBase {

		protected override UseStream Stream => UseStream.GnuTlsStream;

	}
}
