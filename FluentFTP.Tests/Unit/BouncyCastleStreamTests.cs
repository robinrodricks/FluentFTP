using System;
using System.Net.Sockets;
using System.Security.Authentication;
using FluentFTP.BouncyCastle;
using FluentFTP.Streams;
using Xunit;

namespace FluentFTP.Tests.Unit {
	public class BouncyCastleStreamTests {
		[Fact]
		public void SessionResumptionIsRequiredByDefault() {
			Assert.True(new BouncyCastleFtpConfig().RequireSessionResumption);
		}

		[Fact]
		public void LegacyResumptionIsOptIn() {
			Assert.False(new BouncyCastleFtpConfig().AllowLegacyResumption);
		}

		[Fact]
		public void UninitializedStreamReportsClosedCapabilities() {
			using (var stream = new BouncyCastleFtpStream()) {
				Assert.False(stream.CanRead());
				Assert.False(stream.CanWrite());
				Assert.Equal(SslProtocols.None, stream.GetSslProtocol());
				Assert.Equal("0x0000", stream.GetCipherSuite());
			}
		}

		[Fact]
		public void UninitializedBaseStreamIsRejected() {
			using (var stream = new BouncyCastleFtpStream()) {
				Assert.Throws<InvalidOperationException>(() => stream.GetBaseStream());
			}
		}

		[Fact]
		public void DisposeIsIdempotent() {
			var stream = new BouncyCastleFtpStream();
			stream.Dispose();
			stream.Dispose();
		}

		[Fact]
		public void InitializationRejectsWrongConfigurationType() {
			using (var stream = new BouncyCastleFtpStream())
			using (var socket = NewSocket()) {
				Assert.Throws<ArgumentException>(() => stream.Init(
					new FtpClient(),
					"localhost",
					socket,
					m_acceptCertificate,
					true,
					null!,
					new UnsupportedStreamConfig()));
			}
		}

		[Fact]
		public void DataConnectionRequiresBouncyCastleControlStream() {
			using (var stream = new BouncyCastleFtpStream())
			using (var socket = NewSocket()) {
				Assert.Throws<InvalidOperationException>(() => stream.Init(
					new FtpClient(),
					"localhost",
					socket,
					m_acceptCertificate,
					false,
					null!,
					new BouncyCastleFtpConfig()));
			}
		}

		[Fact]
		public void DataConnectionRequiresResumableControlSessionByDefault() {
			using (var stream = new BouncyCastleFtpStream())
			using (var controlStream = new BouncyCastleFtpStream())
			using (var socket = NewSocket()) {
				Assert.Throws<InvalidOperationException>(() => stream.Init(
					new FtpClient(),
					"localhost",
					socket,
					m_acceptCertificate,
					false,
					controlStream,
					new BouncyCastleFtpConfig()));
			}
		}

		[Fact]
		public void DisposedStreamRejectsInitialization() {
			var stream = new BouncyCastleFtpStream();
			stream.Dispose();

			using (var socket = NewSocket()) {
				Assert.Throws<ObjectDisposedException>(() => stream.Init(
					new FtpClient(),
					"localhost",
					socket,
					m_acceptCertificate,
					true,
					null!,
					new BouncyCastleFtpConfig()));
			}
		}

		private static Socket NewSocket() => new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

		private static readonly CustomRemoteCertificateValidationCallback m_acceptCertificate =
			(_, _, _, _) => true;

		private sealed class UnsupportedStreamConfig : IFtpStreamConfig {
		}
	}
}
