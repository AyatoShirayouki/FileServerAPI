using OneBitSoftware.Utilities;

namespace FileShareLibrary
{
	public interface IContentProvider<in TKey>
		where TKey : struct, IEquatable<TKey>
	{
		Task<OperationResult> StoreAsync(TKey id, StreamInfo fileContent, CancellationToken cancellationToken);
		Task<OperationResult<bool>> ExistsAsync(TKey id, CancellationToken cancellationToken);
		Task<OperationResult<StreamInfo>> GetAsync(TKey id, CancellationToken cancellationToken);
		Task<OperationResult<byte[]>> GetBytesAsync(TKey id, CancellationToken cancellationToken);
		Task<OperationResult> UpdateAsync(TKey id, StreamInfo fileContent, CancellationToken cancellationToken);
		Task<OperationResult> DeleteAsync(TKey id, CancellationToken cancellationToken);
		Task<OperationResult<string>> GetHashAsync(TKey id, CancellationToken cancellationToken);
	}
}
