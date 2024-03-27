using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OneBitSoftware.Utilities;
using OneBitSoftware.Utilities.Errors;
using System.Text;

namespace FileShareLibrary.Tests
{
	public class FileShareLibrary_Tests
	{
		private readonly IConfigurationRoot _configuration;
		private readonly string _fileSharePath;

		public FileShareLibrary_Tests()
		{
			_configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.Build();

			_fileSharePath = _configuration.GetValue<string>("FileSharePath");
		}

		[Fact]
		public async Task StoreAsync_SuccessfullyStoresFile_ReturnsSuccess()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var fileName = "StoreAsync_SuccessfullyStoresFile_ReturnsSuccess.txt";
			var mockStreamInfo = new StreamInfo { Stream = new MemoryStream(), Length = 100 };

			var result = await provider.StoreAsync(fileName, mockStreamInfo, CancellationToken.None);

			Assert.True(result.Success);
			File.Delete(Path.Combine(_fileSharePath, fileName));
		}

		[Fact]
		public async Task StoreAsync_WithCancellation_ThrowsOperationCanceledException()
		{
			var cancellationTokenSource = new CancellationTokenSource();
			cancellationTokenSource.Cancel();

			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "StoreAsync_WithCancellation_ThrowsOperationCanceledException.txt";
			var mockStreamInfo = new StreamInfo { Stream = new MemoryStream(), Length = 100 };

			await Assert.ThrowsAsync<OperationCanceledException>(() =>
				provider.StoreAsync(testFileId, mockStreamInfo, cancellationTokenSource.Token));
		}

		[Fact]
		public async Task UpdateAsync_FileDoesNotExist_ReturnsError()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "UpdateAsync_FileDoesNotExist_ReturnsError.txt";
			var mockStreamInfo = new StreamInfo { Stream = new MemoryStream(), Length = 100 };

			var result = await provider.UpdateAsync(testFileId, mockStreamInfo, CancellationToken.None);

			Assert.False(result.Success);
			Assert.NotEmpty(result.Errors);
		}

		[Fact]
		public async Task GetAsync_FileExists_ReturnsStreamInfo()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "GetAsync_FileExists_ReturnsStreamInfo.txt";
			var testFilePath = Path.Combine(_fileSharePath, testFileId.ToString());

			await File.WriteAllTextAsync(testFilePath, "Test content");

			OperationResult<StreamInfo> result = null;

			try
			{
				result = await provider.GetAsync(testFileId, CancellationToken.None);

				Assert.True(result.Success);
				Assert.NotNull(result.ResultObject);

				if (result.ResultObject != null)
				{
					result.ResultObject.Stream.Dispose();
				}
			}
			finally
			{
				if (result?.ResultObject?.Stream != null)
				{
					result.ResultObject.Stream.Dispose();
				}
				if (File.Exists(testFilePath))
				{
					File.Delete(testFilePath);
				}
			}
		}

		[Fact]
		public async Task GetBytesAsync_LargeFile_ReturnsBytesWithCorrectLength()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "GetBytesAsync_LargeFile_ReturnsBytesWithCorrectLength.txt";
			var testFilePath = Path.Combine(_fileSharePath, testFileId.ToString());

			const int expectedFileSize = 1024 * 1024 * 50;
			byte[] data = new byte[expectedFileSize];
			new Random().NextBytes(data);

			await File.WriteAllBytesAsync(testFilePath, data);

			try
			{
				var result = await provider.GetBytesAsync(testFileId, CancellationToken.None);
				
				Assert.True(result.Success);

				if (result.ResultObject.Length != expectedFileSize)
				{
					Assert.True(false, $"The retrieved file size ({result.ResultObject.Length} bytes) does not match the expected size ({expectedFileSize} bytes).");
				}
			}
			finally
			{
				if (File.Exists(testFilePath))
				{
					File.Delete(testFilePath);
				}
			}
		}

		[Fact]
		public async Task GetAsync_FileNotFound_ReturnsError()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "GetAsync_FileNotFound_ReturnsError.txt";

			var result = await provider.GetAsync(testFileId, CancellationToken.None);

			Assert.False(result.Success);
			Assert.Contains(result.Errors, e => e.Message.Contains("not found") || e.Message.Contains("does not exist"));
		}

		[Fact]
		public async Task GetBytesAsync_ExtremelyLargeFile_ReturnsErrorOrHandlesGracefully()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "GetBytesAsync_ExtremelyLargeFile_ReturnsErrorOrHandlesGracefully.txt";
			var testFilePath = Path.Combine(_fileSharePath, testFileId.ToString());

			const int largeFileSize = 1024 * 1024 * 50;
			byte[] data = new byte[largeFileSize];
			new Random().NextBytes(data);

			await File.WriteAllBytesAsync(testFilePath, data);

			try
			{
				var result = await provider.GetBytesAsync(testFileId, CancellationToken.None);

				Assert.True(result.Success || ContainsExpectedLargeFileError(result.Errors));

				if (result.Success)
				{
					Assert.Equal(largeFileSize, result.ResultObject.Length);
				}
			}
			finally
			{
				if (File.Exists(testFilePath))
				{
					File.Delete(testFilePath);
				}
			}
		}

		private bool ContainsExpectedLargeFileError(IEnumerable<IOperationError> errors)
		{
			return errors.Any(e => e.Message.Contains("File too large") || e.Message.Contains("Out of memory"));
		}

		[Fact]
		public async Task UpdateAsync_SimultaneousAccess_ReturnsTrue()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "UpdateAsync_SimultaneousAccess_ReturnsTrue.txt";
			var testFilePath = Path.Combine(_fileSharePath, testFileId.ToString());

			await File.WriteAllTextAsync(testFilePath, "Initial content");

			var mockStreamInfo = new StreamInfo { Stream = new MemoryStream(), Length = 100 };

			try
			{
				var result = await provider.UpdateAsync(testFileId, mockStreamInfo, CancellationToken.None);
				Assert.True(result.Success);
			}
			finally
			{
				if (File.Exists(testFilePath))
				{
					File.Delete(testFilePath);
				}
			}
		}

		[Fact]
		public async Task GetBytesAsync_WithCancellation_ThrowsOperationCanceledException()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "GetBytesAsync_WithCancellation_ThrowsOperationCanceledException.txt";
			var cancellationTokenSource = new CancellationTokenSource();

			cancellationTokenSource.Cancel();

			await Assert.ThrowsAsync<TaskCanceledException>(() =>
				provider.GetBytesAsync(testFileId, cancellationTokenSource.Token));
		}

		[Fact]
		public async Task ExistsAsync_FileExists_ReturnsTrue()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "ExistsAsync_FileExists_ReturnsTrue.txt";
			var testFilePath = Path.Combine(_fileSharePath, testFileId.ToString());

			await File.WriteAllTextAsync(testFilePath, "Test content");

			try
			{
				var result = await provider.ExistsAsync(testFileId, CancellationToken.None);

				Assert.True(result.Success); 
			}
			finally
			{
				if (File.Exists(testFilePath))
				{
					File.Delete(testFilePath);
				}
			}
		}

		[Fact]
		public async Task UpdateAsync_WithCancellation_ThrowsOperationCanceledException()
		{
			var cancellationTokenSource = new CancellationTokenSource();
			cancellationTokenSource.Cancel();

			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "UpdateAsync_WithCancellation_ThrowsOperationCanceledException.txt";
			var mockStreamInfo = new StreamInfo { Stream = new MemoryStream(), Length = 100 };

			await Assert.ThrowsAsync<OperationCanceledException>(() =>
				provider.UpdateAsync(testFileId, mockStreamInfo, cancellationTokenSource.Token));
		}

		[Fact]
		public async Task UpdateAsync_SuccessfullyUpdatesFile_ReturnsSuccess()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "UpdateAsync_SuccessfullyUpdatesFile_ReturnsSuccess.txt";
			var testFilePath = Path.Combine(_fileSharePath, testFileId.ToString());

			await File.WriteAllTextAsync(testFilePath, "Initial content");
			var mockStreamInfo = new StreamInfo { Stream = new MemoryStream(Encoding.UTF8.GetBytes("Updated content")), Length = 100 };

			try
			{
				var result = await provider.UpdateAsync(testFileId, mockStreamInfo, CancellationToken.None);

				Assert.True(result.Success, "Expected the update operation to be successful.");
			}
			finally
			{
				if (File.Exists(testFilePath))
				{
					File.Delete(testFilePath);
				}
			}
		}

		[Fact]
		public async Task ExistsAsync_WithCancellation_ThrowsOperationCanceledException()
		{
			var cancellationTokenSource = new CancellationTokenSource();
			cancellationTokenSource.Cancel();

			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "ExistsAsync_WithCancellation_ThrowsOperationCanceledException.txt";

			await Assert.ThrowsAsync<OperationCanceledException>(() =>
				provider.ExistsAsync(testFileId, cancellationTokenSource.Token));
		}

		[Fact]
		public async Task StoreAsync_InvalidInput_ReturnsError()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "StoreAsync_InvalidInput_ReturnsError.txt";

			var mockStreamInfo = new StreamInfo { Stream = null, Length = 0 }; 

			var result = await provider.StoreAsync(testFileId, mockStreamInfo, CancellationToken.None);

			Assert.False(result.Success, "Expected the store operation to fail due to invalid input.");
		}

		[Fact]
		public async Task GetAsync_WithCancellation_ThrowsOperationCanceledException()
		{
			var cancellationTokenSource = new CancellationTokenSource();
			cancellationTokenSource.Cancel();

			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "GetAsync_WithCancellation_ThrowsOperationCanceledException.txt";

			await Assert.ThrowsAsync<OperationCanceledException>(() =>
				provider.GetAsync(testFileId, cancellationTokenSource.Token));
		}

		[Fact]
		public async Task ExistsAsync_FileDoesNotExist_ReturnsFalse()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "ExistsAsync_FileDoesNotExist_ReturnsFalse.txt";

			var result = await provider.ExistsAsync(testFileId, CancellationToken.None);

			Assert.False(result.ResultObject, "Expected file to not exist.");
			Assert.False(result.Success, "Expected the operation to indicate failure.");
		}

		[Fact]
		public async Task DeleteAsync_FileExists_DeletesFileSuccessfully()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "DeleteAsync_FileExists_DeletesFileSuccessfully.txt";
			var testFilePath = Path.Combine(_fileSharePath, testFileId.ToString());

			await File.WriteAllTextAsync(testFilePath, "Test content");

			var result = await provider.DeleteAsync(testFileId, CancellationToken.None);

			Assert.True(result.Success, "Expected the delete operation to succeed.");
			Assert.False(File.Exists(testFilePath), "Expected the file to be deleted.");
		}

		[Fact]
		public async Task DeleteAsync_FileDoesNotExist_ReturnsError()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "DeleteAsync_FileDoesNotExist_ReturnsError.txt";

			var result = await provider.DeleteAsync(testFileId, CancellationToken.None);

			Assert.False(result.Success, "Expected the delete operation to fail due to non-existent file.");
			Assert.Contains(result.Errors, e => e.Message.Contains("does not exist"));
		}

		[Fact]
		public async Task DeleteAsync_WithCancellation_ThrowsOperationCanceledException()
		{
			var cancellationTokenSource = new CancellationTokenSource();
			cancellationTokenSource.Cancel();

			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "DeleteAsync_WithCancellation_ThrowsOperationCanceledException.txt";

			await Assert.ThrowsAsync<OperationCanceledException>(() =>
				provider.DeleteAsync(testFileId, cancellationTokenSource.Token));
		}


		[Fact]
		public async Task GetHashAsync_FileExists_ComputesHashSuccessfully()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "GetHashAsync_FileExists_ComputesHashSuccessfully.txt";
			var testFilePath = Path.Combine(_fileSharePath, testFileId.ToString());

			await File.WriteAllTextAsync(testFilePath, "Test content");

			var result = await provider.GetHashAsync(testFileId, CancellationToken.None);

			Assert.True(result.Success, "Expected the hash computation to succeed.");
			Assert.NotNull(result.ResultObject);
		}

		[Fact]
		public async Task GetHashAsync_FileDoesNotExist_ReturnsError()
		{
			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "GetHashAsync_FileDoesNotExist_ReturnsError.txt";

			var result = await provider.GetHashAsync(testFileId, CancellationToken.None);

			Assert.False(result.Success, "Expected the hash computation to fail due to non-existent file.");
			Assert.Contains(result.Errors, e => e.Message.Contains("does not exist"));
		}

		[Fact]
		public async Task GetHashAsync_WithCancellation_ThrowsOperationCanceledException()
		{
			var cancellationTokenSource = new CancellationTokenSource();
			cancellationTokenSource.Cancel();

			var mockLogger = new Mock<ILogger<FileShareContentProvider>>();
			var provider = new FileShareContentProvider(_fileSharePath, mockLogger.Object);
			var testFileId = "GetHashAsync_WithCancellation_ThrowsOperationCanceledException.txt";

			await Assert.ThrowsAsync<OperationCanceledException>(() =>
				provider.GetHashAsync(testFileId, cancellationTokenSource.Token));
		}

	}
}
