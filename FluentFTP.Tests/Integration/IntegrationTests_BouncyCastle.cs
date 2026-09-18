using FluentFTP.Tests.Integration.System;

namespace FluentFTP.Tests.Integration {

	// vsftpd(ssl) and proftpd(ssl) REFUSE data connections that do not resume the control session, so they
	// are the IDEAL candidates for testing the BouncyCastle stream. All other servers should of course work
	// but do not prove the BouncyCastle streams special capability of resuming the control session on data connections.
	public class IntegrationTests_BouncyCastle : IntegrationTestsBase {

		protected override UseStream Stream => UseStream.BouncyCastleStream;

	}
}
