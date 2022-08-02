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

namespace FluentFTP.Tests.Integration {

	/// <summary>
	/// All tests must use [SkippableFact] or [SkippableTheory] to allow "all tests" to run successfully on a developer machine without Docker.
	/// </summary>
	public class ListingTests : IntegrationTestSuite {

		public ListingTests(DockerFtpServerFixture fixture) : base(fixture) { }


		[SkippableFact]
		public async Task GetListingAsync() {
			using var client = GetConnectedClient();
			var bytes = Encoding.UTF8.GetBytes("a");
			const string directory = "/GetListingAsync/";
			const string fileNameInRoot = "GetListingAsync.txt";
			const string fileNameInDirectory = "GetListingAsyncInDirectory.txt";
			await client.UploadBytesAsync(bytes, fileNameInRoot);
			await client.UploadBytesAsync(bytes, directory + fileNameInDirectory, createRemoteDir: true);

			var listRoot = await client.GetListingAsync();
			Assert.Contains(listRoot, f => f.Name == fileNameInRoot);

			var listDirectory = await client.GetListingAsync(directory);
			Assert.Contains(listDirectory, f => f.Name == fileNameInDirectory);

			await client.SetWorkingDirectoryAsync(directory);
			var listCurrentDirectory = await client.GetListingAsync();
			Assert.Contains(listCurrentDirectory, f => f.Name == fileNameInDirectory);
		}

		[SkippableFact]
		public void GetListing() {
			using var client = GetConnectedClient();
			var bytes = Encoding.UTF8.GetBytes("a");
			const string directory = "/GetListing/";
			const string fileNameInRoot = "GetListing.txt";
			const string fileNameInDirectory = "GetListingInDirectory.txt";
			client.UploadBytes(bytes, fileNameInRoot);
			client.UploadBytes(bytes, directory + fileNameInDirectory, createRemoteDir: true);

			var listRoot = client.GetListing();
			Assert.Contains(listRoot, f => f.Name == fileNameInRoot);

			var listDirectory = client.GetListing(directory);
			Assert.Contains(listDirectory, f => f.Name == fileNameInDirectory);

			client.SetWorkingDirectory(directory);
			var listCurrentDirectory = client.GetListing();
			Assert.Contains(listCurrentDirectory, f => f.Name == fileNameInDirectory);
		}

	}
}