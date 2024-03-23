# Project Documentation
## Overview
This project, FileShareAPI, is designed to manage file operations such as upload, download, deletion, and updates on a file share system. It leverages an ASP.NET Core Minimal API setup for handling HTTP requests and responses, alongside a library, FileShareLibrary, that encapsulates file operation logic.

## Components
### The project consists of two main components:

* __FileShareAPI__: A web API that exposes endpoints for file operations.
* __FileShareLibrary__: A class library that provides the core functionality for file operations.
  
## FileShareAPI
The FileShareAPI component defines HTTP endpoints for uploading, downloading, updating, deleting, checking existence, retrieving bytes, and computing hashes of files. It uses the FileShareLibrary for the implementation of these operations.

### Configuration
The API is configured to handle files up to 50 MB in size, as defined in the ConfigureKestrel server options. The file share path is read from configuration settings, allowing dynamic determination of the file storage location.

### Endpoints
POST /upload: Uploads a file to the file share. It expects an IFormFile in the request and uses the StoreAsync method of the IContentProvider<Guid> to save the file.

GET /download/{fileId}: Downloads a file by its GUID. It uses the GetAsync method to retrieve the file stream and returns it as an application/octet-stream response.

DELETE /delete/{fileId}: Deletes a file by its GUID. It calls the DeleteAsync method to remove the file from the file share.

GET /exists/{fileId}: Checks if a file exists by its GUID. It utilizes the ExistsAsync method to verify the presence of the file.

PUT /update/{fileId}: Updates an existing file with new content. This endpoint expects an IFormFile and uses the UpdateAsync method to overwrite the existing file.

GET /bytes/{fileId}: Retrieves the byte array of a file. This endpoint leverages the GetBytesAsync method to fetch the file's bytes.

GET /hash/{fileId}: Computes and returns the SHA-256 hash of a file. It utilizes the GetHashAsync method to generate the hash.

Each endpoint is configured to disable antiforgery token validation and allow anonymous access for simplicity. Depending on the application's security requirements, these settings can be adjusted.

## FileShareLibrary
The FileShareLibrary provides the implementation of file operations. It defines the IContentProvider<Guid> interface and its implementation, FileShareContentProvider, to perform actions on files identified by GUIDs.

## FileShareContentProvider
This class implements the IContentProvider<Guid> interface, handling the logic for file storage, retrieval, updating, and deletion. It determines file extensions based on file content, supporting various file types, and performs operations in a designated file share path.

### Key Methods

#### StoreAsync(Guid id, StreamInfo fileContent, CancellationToken cancellationToken)
Stores a file in the specified file share path. It generates a unique file name using the provided id and determines the file extension based on the content's signature to support various file types correctly. This method ensures files are stored securely and reliably, even in the face of network or system failures, by leveraging .NET's file stream capabilities.

##### Parameters:
* __id__: A Guid serving as a unique identifier for the file.
* __fileContent__: A StreamInfo object containing the file's stream and length.
* __cancellationToken__: A CancellationToken for handling request cancellations.
* __Returns__: An OperationResult indicating the success or failure of the operation, including error messages if applicable.
  
#### GetAsync(Guid id, CancellationToken cancellationToken)
Retrieves a file's stream and information for downloading by matching the id with files in the storage path. This method is optimized to handle large files efficiently, minimizing memory usage by streaming file contents directly from disk to the network.

##### Parameters:
* __id__: The unique identifier of the file to retrieve.
* __cancellationToken__: A token for canceling the operation if necessary.
* __Returns__: An OperationResult<StreamInfo> containing the file's stream and size if successful, or error information if not.
  
### UpdateAsync(Guid id, StreamInfo fileContent, CancellationToken cancellationToken)
Updates an existing file with new content. It first checks for the file's existence by id and then overwrites it with the new content provided in fileContent. This method is critical for maintaining the integrity and up-to-dateness of files in the system.

#### Parameters:
* __id__: The Guid identifying the file to update.
* __fileContent__: The new content to write to the file, encapsulated in a StreamInfo object.
* __cancellationToken__: Allows the operation to be cancelled.
* __Returns__: An OperationResult indicating the outcome of the update operation.
  
### DeleteAsync(Guid id, CancellationToken cancellationToken)
Removes a file from the file share. It searches for the file by id and deletes it if found. This method ensures that all traces of the file are removed from the system, freeing up space and maintaining the cleanliness of the storage area.

#### Parameters:
* __id__: The Guid of the file to delete.
* __cancellationToken__: A token that can be used to request cancellation of the operation.
* __Returns__: An OperationResult indicating whether the deletion was successful or if errors occurred.

### ExistsAsync(Guid id, CancellationToken cancellationToken)
Checks if a file exists in the file share. By searching for a file with the given id, this method quickly determines file presence, which is essential for validation checks before attempting to access or manipulate files.

#### Parameters:
* __id__: The unique identifier of the file to check.
* __cancellationToken__: A cancellation token for the operation.
* __Returns__: An OperationResult<bool> indicating the existence of the file.

### GetHashAsync(Guid id, CancellationToken cancellationToken)
Computes and returns the SHA-256 hash of a file. This method is vital for verifying the integrity of files and ensuring they have not been tampered with. It reads the file stream associated with the id, computes its hash, and returns the hash value as a string.

#### Parameters:
* __id__: The unique identifier of the file to hash.
* __cancellationToken__: A token for cancelling the operation.
* __Returns__: An OperationResult<string> containing the file's hash value or an error if the file does not exist or an exception occurs.

### StreamInfo
A support class that encapsulates file stream information, including its length and the stream itself.

### OperationResult
A generic class used throughout the library to encapsulate the outcome of operations, including success or failure, error messages, and the result object for successful operations.

## FileShareLibrary.Test
This component contains unit tests for the FileShareLibrary. It tests the functionality of file operations, including success and failure scenarios, using mock data and verifying the expected outcomes.

### Test Setup
Tests are configured with a mock file share path and use the Moq library to mock dependencies. They cover various scenarios, including file existence, file size limitations, and error handling.

### Key Test Cases

* __StoreAsync_SuccessfullyStoresFile_ReturnsSuccess__: Verifies that files are stored successfully.
* __GetAsync_FileExists_ReturnsStreamInfo__: Checks that files can be retrieved correctly.
* __UpdateAsync_FileDoesNotExist_ReturnsError__: Ensures error handling for attempts to update non-existent files.
* __DeleteAsync_FileExists_DeletesFileSuccessfully__: Confirms that files can be deleted.
