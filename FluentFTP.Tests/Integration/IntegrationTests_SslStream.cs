using FluentFTP.Tests.Integration.System;

namespace FluentFTP.Tests.Integration {

	public class IntegrationTests_SslStream : IntegrationTestsBase {

		protected override UseStream Stream => UseStream.SslStream;

	}
}
