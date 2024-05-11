using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentFTP.Xunit.Docker;
using FluentFTP.Xunit.Attributes;
using FluentFTP.Tests.Integration.System;
using FluentFTP.Exceptions;

namespace FluentFTP.Tests.Integration.Tests {

	internal class FileTransferTests : IntegrationTestSuite {

		public FileTransferTests(DockerFtpServer fixture, UseStream stream) : base(fixture, stream) { }

		/// <summary>
		/// Main entrypoint executed for all types of FTP servers.
		/// </summary>
		public override void RunAllTests() {
			UploadDownloadBytes();
			UploadDownloadStream();
			UploadDownloadDirect();
			DownloadMissing();
		}

		/// <summary>
		/// Main entrypoint executed for all types of FTP servers.
		/// </summary>
		public async override Task RunAllTestsAsync() {
			await UploadDownloadBytesAsync();
			await UploadDownloadStreamAsync();
			await UploadDownloadDirectAsync();
			await DownloadMissingAsync();
		}


		public async Task DownloadMissingAsync() {

			using var client = await GetConnectedAsyncClient();
			var filePath = "/no/such/file.txt";
			var dirPath = "/no/such/dir/";

			// DownloadDirectory should crash with FtpMissingObjectException if the dir does not exist
			try {
				await client.DownloadDirectory("/DownloadMissingAsync/", dirPath);
			}
			catch (Exception ex) {
				Assert.IsType<FtpMissingObjectException>(ex);
			}

			// DownloadFile should crash with FtpMissingObjectException if the file does not exist
			try {
				await client.DownloadFile("/DownloadMissingAsync/myfile.txt", filePath);
			}
			catch (Exception ex) {
				Assert.IsType<FtpMissingObjectException>(ex);
			}

			// DownloadBytes should crash with FtpMissingObjectException if the file does not exist
			try {
				await client.DownloadBytes(filePath, 0);
			}
			catch (Exception ex) {
				Assert.IsType<FtpMissingObjectException>(ex);
			}

			// DownloadStream should crash with FtpMissingObjectException if the file does not exist
			try {
				using var stream = new MemoryStream();
				await client.DownloadStream(stream, filePath);
			}
			catch (Exception ex) {
				Assert.IsType<FtpMissingObjectException>(ex);
			}

			// DownloadFiles should not crash, but should return the FtpMissingObjectException inside the first result's Exception property
			var result = await client.DownloadFiles("/DownloadMissingAsync/", new List<string> { filePath });
			Assert.True(result[0].IsFailed);
			Assert.IsType<FtpMissingObjectException>(result[0].Exception);

		}

		public void DownloadMissing() {

			using var client = GetConnectedClient();
			var filePath = "/no/such/file.txt";
			var dirPath = "/no/such/dir/";

			// DownloadDirectory should crash with FtpMissingObjectException if the dir does not exist
			try {
				client.DownloadDirectory("/DownloadMissingAsync/", dirPath);
			}
			catch (Exception ex) {
				Assert.IsType<FtpMissingObjectException>(ex);
			}

			// DownloadFile should crash with FtpMissingObjectException if the file does not exist
			try {
				client.DownloadFile("/DownloadMissingAsync/myfile.txt", filePath);
			}
			catch (Exception ex) {
				Assert.IsType<FtpMissingObjectException>(ex);
			}

			// DownloadBytes should crash with FtpMissingObjectException if the file does not exist
			try {
				client.DownloadBytes(out byte[] result2, filePath, 0);
			}
			catch (Exception ex) {
				Assert.IsType<FtpMissingObjectException>(ex);
			}

			// DownloadStream should crash with FtpMissingObjectException if the file does not exist
			try {
				using var stream = new MemoryStream();
				client.DownloadStream(stream, filePath);
			}
			catch (Exception ex) {
				Assert.IsType<FtpMissingObjectException>(ex);
			}

			// DownloadFiles should not crash, but should return the FtpMissingObjectException inside the first result's Exception property
			var result = client.DownloadFiles("/DownloadMissingAsync/", new List<string> { filePath });
			Assert.True(result[0].IsFailed);
			Assert.IsType<FtpMissingObjectException>(result[0].Exception);

		}
		public async Task UploadDownloadBytesAsync() {

			const string content = "Hello World!";
			var bytes = Encoding.UTF8.GetBytes(content);

			using var client = await GetConnectedAsyncClient();

			const string path = "/UploadDownloadBytesAsync/helloworld.txt";
			var uploadStatus = await client.UploadBytes(bytes, path, createRemoteDir: true);
			Assert.Equal(FtpStatus.Success, uploadStatus);

			var outBytes = await client.DownloadBytes(path, CancellationToken.None);
			Assert.NotNull(outBytes);

			var outContent = Encoding.UTF8.GetString(outBytes);
			Assert.Equal(content, outContent);
		}


		public void UploadDownloadBytes() {

			const string content = "Hello World!";
			var bytes = Encoding.UTF8.GetBytes(content);

			using var client = GetConnectedClient();

			const string path = "/UploadDownloadBytes/helloworld.txt";
			var uploadStatus = client.UploadBytes(bytes, path, createRemoteDir: true);
			Assert.Equal(FtpStatus.Success, uploadStatus);

			var success = client.DownloadBytes(out var outBytes, path);
			Assert.True(success);

			var outContent = Encoding.UTF8.GetString(outBytes);
			Assert.Equal(content, outContent);
		}


		public async Task UploadDownloadStreamAsync() {

			const string content = "Hello World!";
			using var stream = new MemoryStream();
			using var streamWriter = new StreamWriter(stream, Encoding.UTF8);
			await streamWriter.WriteAsync(content);
			await streamWriter.FlushAsync();
			stream.Position = 0;

			const string path = "/UploadDownloadStreamAsync/helloworld.txt";
			using var client = await GetConnectedAsyncClient();
			var uploadStatus = await client.UploadStream(stream, path, createRemoteDir: true);
			Assert.Equal(FtpStatus.Success, uploadStatus);

			using var outStream = new MemoryStream();
			var success = await client.DownloadStream(outStream, path);
			Assert.True(success);

			outStream.Position = 0;
			using var streamReader = new StreamReader(outStream, Encoding.UTF8);
			var outContent = await streamReader.ReadToEndAsync();
			Assert.Equal(content, outContent);
		}


		public void UploadDownloadStream() {

			const string content = "Hello World!";
			using var stream = new MemoryStream();
			using var streamWriter = new StreamWriter(stream, Encoding.UTF8);
			streamWriter.Write(content);
			streamWriter.Flush();
			stream.Position = 0;

			const string path = "/UploadDownloadStream/helloworld.txt";
			using var client = GetConnectedClient();
			var uploadStatus = client.UploadStream(stream, path, createRemoteDir: true);
			Assert.Equal(FtpStatus.Success, uploadStatus);

			using var outStream = new MemoryStream();
			var success = client.DownloadStream(outStream, path);
			Assert.True(success);

			outStream.Position = 0;
			using var streamReader = new StreamReader(outStream, Encoding.UTF8);
			var outContent = streamReader.ReadToEnd();
			Assert.Equal(content, outContent);
		}


		public async Task UploadDownloadDirectAsync() {

			const string content = "Hello World!";
			ReadOnlyMemory<byte> buffer = Encoding.UTF8.GetBytes(content);

			const string directory = "UploadDownloadDirectAsync";
			const string path = $"/{directory}/helloworld.txt";
			using var client = await GetConnectedAsyncClient();
			await client.CreateDirectory(directory);
			await using (var dest = await client.OpenWrite(path)) {
				await dest.WriteAsync(buffer);
			}

			Memory<byte> outBuffer = new byte[buffer.Length];
			var readBuffer = outBuffer;
			await using (var src = await client.OpenRead(path)) {
				var readCount = 0;
				while (readBuffer.Length > 0 && (readCount = await src.ReadAsync(readBuffer)) > 0) {
					readBuffer = readBuffer.Slice(readCount);
				}
			}

			var outContent = Encoding.UTF8.GetString(outBuffer.Span);
			Assert.Equal(content, outContent);
		}


		public void UploadDownloadDirect() {

			const string content = "Hello World!";
			ReadOnlySpan<byte> buffer = Encoding.UTF8.GetBytes(content);

			const string directory = "UploadDownloadDirect";
			const string path = $"/{directory}/helloworld.txt";
			using var client = GetConnectedClient();
			client.CreateDirectory(directory);
			using (var dest = client.OpenWrite(path)) {
				dest.Write(buffer);
			}

			Span<byte> outBuffer = new byte[buffer.Length];
			var readBuffer = outBuffer;
			using (var src = client.OpenRead(path)) {
				var readCount = 0;
				while (readBuffer.Length > 0 && (readCount = src.Read(readBuffer)) > 0) {
					readBuffer = readBuffer.Slice(readCount);
				}
			}

			var outContent = Encoding.UTF8.GetString(outBuffer);
			Assert.Equal(content, outContent);
		}

	}
}