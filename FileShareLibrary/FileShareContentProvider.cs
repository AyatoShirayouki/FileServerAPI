using Microsoft.Extensions.Logging;
using OneBitSoftware.Utilities;

namespace FileShareLibrary
{
	public class FileShareContentProvider : IContentProvider<StringKey>
	{
		private readonly string _fileSharePath;
		private readonly ILogger<FileShareContentProvider> _logger;

		public FileShareContentProvider(string fileSharePath, ILogger<FileShareContentProvider> logger)
		{
			_fileSharePath = fileSharePath ?? throw new ArgumentNullException(nameof(fileSharePath));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task<OperationResult> StoreAsync(StringKey fileName, StreamInfo fileContent, CancellationToken cancellationToken)
		{
			var operationResult = new OperationResult();
			var filePath = Path.Combine(_fileSharePath, fileName);

			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					fileContent.Stream.Position = 0;
					await fileContent.Stream.CopyToAsync(fileStream, cancellationToken);
					operationResult.AddSuccessMessage($"File {fileName} stored successfully.");
				}
			}
			catch (Exception ex)
			{
				operationResult.AppendError($"An error occurred: {ex.Message}");
			}

			return operationResult;
		}

		public async Task<OperationResult<StreamInfo>> GetAsync(StringKey fileName, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var operationResult = new OperationResult<StreamInfo>();
			var filePath = Path.Combine(_fileSharePath, fileName);

			if (!File.Exists(filePath))
			{
				operationResult.AppendError($"File {fileName} does not exist.");
				return operationResult;
			}

			try
			{
				var fileInfo = new FileInfo(filePath);
				var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
				var streamInfo = new StreamInfo
				{
					Length = fileInfo.Length,
					Stream = stream
				};
				operationResult.ResultObject = streamInfo;
				operationResult.AddSuccessMessage($"File {fileName} retrieved successfully.");
			}
			catch (Exception ex)
			{
				operationResult.AppendError($"An error occurred: {ex.Message}");
			}

			return operationResult;
		}

		public async Task<OperationResult<byte[]>> GetBytesAsync(StringKey fileName, CancellationToken cancellationToken)
		{
			var operationResult = new OperationResult<byte[]>();
			var filePath = Path.Combine(_fileSharePath, fileName);

			if (cancellationToken.IsCancellationRequested)
			{
				throw new TaskCanceledException();
			}

			try
			{
				if (!File.Exists(filePath))
				{
					operationResult.AppendError($"File {fileName} does not exist.");
					return operationResult;
				}

				var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
				operationResult.ResultObject = bytes;
				operationResult.AddSuccessMessage($"File {fileName} bytes retrieved successfully.");
			}
			catch (Exception ex)
			{
				operationResult.AppendError($"An error occurred: {ex.Message}");
			}
			return operationResult;
		}

		public async Task<OperationResult> UpdateAsync(StringKey fileName, StreamInfo fileContent, CancellationToken cancellationToken)
		{
			var operationResult = new OperationResult();
			var filePath = Path.Combine(_fileSharePath, fileName);

			cancellationToken.ThrowIfCancellationRequested();

			if (!File.Exists(filePath))
			{
				operationResult.AppendError($"File {fileName} does not exist.");
				return operationResult;
			}

			try
			{
				using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					await fileContent.Stream.CopyToAsync(fileStream, cancellationToken);
					operationResult.AddSuccessMessage($"File {fileName} updated successfully.");
				}
			}
			catch (Exception ex)
			{
				operationResult.AppendError($"An error occurred: {ex.Message}");
			}
			return operationResult;
		}

		public async Task<OperationResult> DeleteAsync(StringKey fileName, CancellationToken cancellationToken)
		{
			var operationResult = new OperationResult();
			var filePath = Path.Combine(_fileSharePath, fileName);

			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				if (!File.Exists(filePath))
				{
					operationResult.AppendError($"File {fileName} does not exist.");
				}
				else
				{
					File.Delete(filePath);
					operationResult.AddSuccessMessage($"File {fileName} deleted successfully.");
				}
			}
			catch (Exception ex)
			{
				operationResult.AppendError($"An error occurred: {ex.Message}");
			}

			return operationResult;
		}

		public async Task<OperationResult<string>> GetHashAsync(StringKey fileName, CancellationToken cancellationToken)
		{
			var operationResult = new OperationResult<string>();
			var filePath = Path.Combine(_fileSharePath, fileName);

			cancellationToken.ThrowIfCancellationRequested();

			if (!File.Exists(filePath))
			{
				operationResult.AppendError($"File {fileName} does not exist.");
				return operationResult;
			}

			try
			{
				using (var stream = File.OpenRead(filePath))
				{
					var hash = System.Security.Cryptography.SHA256.Create();
					byte[] hashBytes = await hash.ComputeHashAsync(stream, cancellationToken);
					operationResult.ResultObject = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
					operationResult.AddSuccessMessage($"File {fileName} hash computed successfully.");
				}
			}
			catch (Exception ex)
			{
				operationResult.AppendError($"An error occurred: {ex.Message}");
			}

			return operationResult;
		}

		public async Task<OperationResult<bool>> ExistsAsync(StringKey fileName, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var operationResult = new OperationResult<bool>();
			var filePath = Path.Combine(_fileSharePath, fileName);

			try
			{
				bool exists = File.Exists(filePath);
				operationResult.ResultObject = exists;
				if (exists)
				{
					operationResult.AddSuccessMessage($"File {fileName} exists.");
				}
				else
				{
					operationResult.AppendError("File does not exist.");
				}
			}
			catch (Exception ex)
			{
				operationResult.AppendException(ex);
				if (ex is OperationCanceledException) throw;
			}

			return operationResult;
		}
	}
}
