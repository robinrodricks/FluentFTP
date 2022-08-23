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

namespace FluentFTP.Tests.Integration.Tests {

	internal class ListingTests : IntegrationTestSuite {

		public ListingTests(DockerFtpServer fixture) : base(fixture) { }


		/// <summary>
		/// Main entrypoint executed for all types of FTP servers.
		/// </summary>
		public override void RunAllTests() {
			GetListing();
		}

		/// <summary>
		/// Main entrypoint executed for all types of FTP servers.
		/// </summary>
		public async override Task RunAllTestsAsync() {
			await GetListingAsync();
			await GetListingAsyncEnumerable();
		}

		private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> asyncEnumerable) {
			var result = new List<T>();
			await foreach (var item in asyncEnumerable) {
				result.Add(item);
			}
			return result;
		}

		public async Task GetListingAsyncEnumerable()
		{
			using var client = GetConnectedClient();
			var bytes = Encoding.UTF8.GetBytes("a");
			const string directory = "/GetListingAsyncEnumerable/";
			const string fileNameInRoot = "GetListingAsync.txt";
			const string fileNameInDirectory = "GetListingAsyncInDirectory.txt";
			await client.UploadBytesAsync(bytes, fileNameInRoot);
			await client.UploadBytesAsync(bytes, directory + fileNameInDirectory, createRemoteDir: true);

			var listRoot = await ToListAsync(client.GetListingAsyncEnumerable());
			Assert.Contains(listRoot, f => f.Name == fileNameInRoot);

			var listDirectory = await ToListAsync(client.GetListingAsyncEnumerable(directory));
			Assert.Contains(listDirectory, f => f.Name == fileNameInDirectory);

			var listRecursive = await ToListAsync(client.GetListingAsyncEnumerable("/", FtpListOption.Recursive));
			Assert.Contains(listRecursive, f => f.Name == fileNameInRoot);
			Assert.Contains(listRecursive, f => f.Name == fileNameInDirectory);

			await client.SetWorkingDirectoryAsync(directory);
			var listCurrentDirectory = await ToListAsync(client.GetListingAsyncEnumerable());
			Assert.Contains(listCurrentDirectory, f => f.Name == fileNameInDirectory);
		}

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

			var listRecursive = await client.GetListingAsync("/", FtpListOption.Recursive);
			Assert.Contains(listRecursive, f => f.Name == fileNameInRoot);
			Assert.Contains(listRecursive, f => f.Name == fileNameInDirectory);

			await client.SetWorkingDirectoryAsync(directory);
			var listCurrentDirectory = await client.GetListingAsync();
			Assert.Contains(listCurrentDirectory, f => f.Name == fileNameInDirectory);
		}

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

			var listRecursive = client.GetListing("/", FtpListOption.Recursive);
			Assert.Contains(listRecursive, f => f.Name == fileNameInRoot);
			Assert.Contains(listRecursive, f => f.Name == fileNameInDirectory);

			client.SetWorkingDirectory(directory);
			var listCurrentDirectory = client.GetListing();
			Assert.Contains(listCurrentDirectory, f => f.Name == fileNameInDirectory);
		}

	}
}