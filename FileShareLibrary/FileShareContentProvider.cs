using Microsoft.Extensions.Logging;
using OneBitSoftware.Utilities;
using OneBitSoftware.Utilities.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileShareLibrary
{
	public class FileShareContentProvider : IContentProvider<Guid>
	{
		private readonly string _fileSharePath;
		private readonly ILogger<FileShareContentProvider> _logger;

		public FileShareContentProvider(string fileSharePath, ILogger<FileShareContentProvider> logger)
		{
			_fileSharePath = fileSharePath ?? throw new ArgumentNullException(nameof(fileSharePath));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task<OperationResult> StoreAsync(Guid id, StreamInfo fileContent, CancellationToken cancellationToken)
		{
			var operationResult = new OperationResult();
			var fileExtension = DetermineFileExtension(fileContent.Stream);
			var fileName = $"{id}{fileExtension}";
			var filePath = Path.Combine(_fileSharePath, fileName);

			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					fileContent.Stream.Position = 0;
					await fileContent.Stream.CopyToAsync(fileStream, cancellationToken);
					operationResult.AddSuccessMessage("File stored successfully.");
				}
			}
			catch (Exception ex)
			{
				operationResult.AppendError($"An error occurred: {ex.Message}");
			}

			return operationResult;
		}

		private string DetermineFileExtension(Stream fileStream)
		{
			if (fileStream == null || fileStream.Length < 8)
			{
				return string.Empty;
			}

			// Define known file signatures
			var fileSignatures = new Dictionary<string, List<byte[]>>
			{
				{ ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
				{ ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
				{ ".jpeg", new List<byte[]>
					{
						new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
						new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 },
						new byte[] { 0xFF, 0xD8, 0xFF, 0xE3 },
					}
				},
				{ ".jpg", new List<byte[]>
					{
						new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
						new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
						new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 },
					}
				},
				{ ".zip", new List<byte[]>
					{
						new byte[] { 0x50, 0x4B, 0x03, 0x04 },
						new byte[] { 0x50, 0x4B, 0x4C, 0x49, 0x54, 0x45 },
						new byte[] { 0x50, 0x4B, 0x53, 0x70, 0x58 },
						new byte[] { 0x50, 0x4B, 0x05, 0x06 },
						new byte[] { 0x50, 0x4B, 0x07, 0x08 },
						new byte[] { 0x57, 0x69, 0x6E, 0x5A, 0x69, 0x70 },
					}
				},
				{ ".rar", new List<byte[]> { new byte[] { 0x52, 0x61, 0x72, 0x21 } } },
				{ ".docx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
				{ ".xlsx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
				{ ".pptx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
				{ ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
				{ ".wav", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 } } }, // Further bytes needed for full signature
				{ ".mp3", new List<byte[]>
					{
						new byte[] { 0x49, 0x44, 0x33 }, // ID3v2 tag
						new byte[] { 0xFF, 0xFB }, // MPEG-1 Layer III or MPEG-2 Layer III
					}
				},
				{ ".txt", new List<byte[]> { } }, // Text files don't have a signature
				};

			byte[] headerBytes = new byte[256];
			fileStream.Read(headerBytes, 0, headerBytes.Length);
			int bytesRead = fileStream.Read(headerBytes, 0, headerBytes.Length);
			fileStream.Position = 0;

			foreach (var fileSignature in fileSignatures)
			{
				foreach (var signature in fileSignature.Value)
				{
					if (!signature.Any() || headerBytes.Take(signature.Length).SequenceEqual(signature))
					{
						if (fileSignature.Key == ".txt" && !IsLikelyTextFile(headerBytes, bytesRead))
						{
							continue;
						}

						return fileSignature.Key;
					}
				}
			}

			if (IsLikelyTextFile(headerBytes, bytesRead))
			{
				return ".txt";
			}

			return string.Empty;
		}


		private bool IsLikelyTextFile(byte[] buffer, int length)
		{
			for (int i = 0; i < length; i++)
			{
				byte b = buffer[i];
				if (b != 9 && b != 10 && b != 13 && (b < 32 || b > 126))
				{
					return false;
				}
			}
			return true;
		}

		public async Task<OperationResult<StreamInfo>> GetAsync(Guid id, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var operationResult = new OperationResult<StreamInfo>();
			var filePattern = $"{id}.*";
			var directoryInfo = new DirectoryInfo(_fileSharePath);
			var file = directoryInfo.GetFiles(filePattern).FirstOrDefault();

			if (file == null)
			{
				operationResult.AppendError($"File with ID {id} does not exist.");
				return operationResult;
			}

			try
			{
				var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
				var streamInfo = new StreamInfo
				{
					Length = file.Length,
					Stream = stream
				};
				operationResult.ResultObject = streamInfo;
				operationResult.AddSuccessMessage("File retrieved successfully.");
			}
			catch (Exception ex)
			{
				operationResult.AppendError($"An error occurred: {ex.Message}");
			}

			return operationResult;
		}

		public async Task<OperationResult<byte[]>> GetBytesAsync(Guid id, CancellationToken cancellationToken)
		{
			var operationResult = new OperationResult<byte[]>();
			var filePath = Path.Combine(_fileSharePath, id.ToString());

			if (cancellationToken.IsCancellationRequested)
			{
				throw new TaskCanceledException();
			}

			try
			{
				if (!File.Exists(filePath))
				{
					operationResult.AppendError($"File with ID {id} does not exist.");
					return operationResult;
				}

				var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
				operationResult.ResultObject = bytes;
				operationResult.AddSuccessMessage("File bytes retrieved successfully.");
			}
			catch (Exception ex)
			{
				operationResult.AppendError($"An error occurred: {ex.Message}");
			}
			return operationResult;
		}

		public async Task<OperationResult> UpdateAsync(Guid id, StreamInfo fileContent, CancellationToken cancellationToken)
		{
			var operationResult = new OperationResult();
			// Use a file pattern to match the GUID with any extension
			var filePattern = $"{id}.*";
			var directoryInfo = new DirectoryInfo(_fileSharePath);
			var files = directoryInfo.GetFiles(filePattern);

			cancellationToken.ThrowIfCancellationRequested();

			if (!files.Any())
			{
				operationResult.AppendError($"File with ID {id} does not exist.");
				return operationResult;
			}

			try
			{
				// Assuming we update the first file if multiple files have the same GUID but different extensions
				var filePath = files.First().FullName;
				using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					await fileContent.Stream.CopyToAsync(fileStream, cancellationToken);
					operationResult.AddSuccessMessage("File updated successfully.");
				}
			}
			catch (Exception ex)
			{
				operationResult.AppendError($"An error occurred: {ex.Message}");
			}
			return operationResult;
		}

		public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
		{
			var operationResult = new OperationResult();
			var filePattern = $"{id}.*";
			var directoryInfo = new DirectoryInfo(_fileSharePath);
			var files = directoryInfo.GetFiles(filePattern);

			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				if (files.Length == 0)
				{
					operationResult.AppendError($"File with ID {id} does not exist.");
				}
				else
				{
					foreach (var file in files)
					{
						File.Delete(file.FullName);
					}
					operationResult.AddSuccessMessage("File deleted successfully.");
				}
			}
			catch (Exception ex)
			{
				operationResult.AppendError($"An error occurred: {ex.Message}");
			}

			return operationResult;
		}

		public async Task<OperationResult<string>> GetHashAsync(Guid id, CancellationToken cancellationToken)
		{
			var operationResult = new OperationResult<string>();
			var filePattern = $"{id}.*";
			var directoryInfo = new DirectoryInfo(_fileSharePath);
			var files = directoryInfo.GetFiles(filePattern);

			cancellationToken.ThrowIfCancellationRequested();

			if (!files.Any())
			{
				operationResult.AppendError($"File with ID {id} does not exist.");
				return operationResult;
			}

			try
			{
				var file = files.First();
				using (var stream = File.OpenRead(file.FullName))
				{
					var hash = System.Security.Cryptography.SHA256.Create();
					byte[] hashBytes = await hash.ComputeHashAsync(stream, cancellationToken);
					operationResult.ResultObject = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
					operationResult.AddSuccessMessage("File hash computed successfully.");
				}
			}
			catch (Exception ex)
			{
				operationResult.AppendError($"An error occurred: {ex.Message}");
			}

			return operationResult;
		}


		public async Task<OperationResult<bool>> ExistsAsync(Guid id, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var operationResult = new OperationResult<bool>();
			var filePattern = $"{id}.*";
			var directoryInfo = new DirectoryInfo(_fileSharePath);
			var files = directoryInfo.GetFiles(filePattern);

			try
			{
				bool exists = files.Any();
				operationResult.ResultObject = exists;
				if (exists)
				{
					operationResult.AddSuccessMessage("File exists.");
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
