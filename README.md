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

* #### StoreAsync_SuccessfullyStoresFile_ReturnsSuccess
  - __Objective__: This test ensures the StoreAsync method can successfully store a file when given valid input.
  - __Methodology__: It mocks a StreamInfo object representing the file to be stored and invokes StoreAsync with a new GUID. The test verifies if the operation is marked as successful.
  - __Verification__: Asserts that the Success property of the returned OperationResult is true, indicating the operation succeeded without issues.
    
* #### StoreAsync_WithCancellation_ThrowsOperationCanceledException
  - __Objective__: Tests whether StoreAsync properly responds to cancellation requests by throwing an OperationCanceledException.
  - __Methodology__: A CancellationToken is canceled before invoking StoreAsync. The method is expected to respect this token and abort the operation.
  - __Verification__: Checks if an OperationCanceledException is thrown, confirming the operation's responsiveness to cancellation.
    
* #### UpdateAsync_FileDoesNotExist_ReturnsError
  - __Objective__: Verifies that UpdateAsync correctly handles attempts to update a non-existent file by returning an error.
  - __Methodology__: Attempts to update a file using a GUID that does not correspond to any existing file. It checks the method's response for error messages.
  - __Verification__: Asserts that the Success flag is false and the Errors collection contains relevant error messages.

* #### GetAsync_FileExists_ReturnsStreamInfo
  - __Objective__: Ensures that GetAsync can retrieve an existing file and return its StreamInfo.
  - __Methodology__: A file is created temporarily, and GetAsync is used to fetch it. The presence and correctness of the returned StreamInfo are then verified.
  - __Verification__: Confirms that Success is true, ResultObject is not null, and the stream within ResultObject matches the content of the file created for the test.

* #### GetBytesAsync_LargeFile_ReturnsBytesWithCorrectLength
  - __Objective__: Tests the ability of GetBytesAsync to handle large files correctly by returning a byte array of the correct length.
  - __Methodology__: Creates a large file and uses GetBytesAsync to retrieve it. The size of the returned byte array is compared to the expected file size.
  - __Verification__: Asserts that the length of the returned byte array matches the size of the file created, ensuring the file's content is correctly read and returned.

* #### GetAsync_FileNotFound_ReturnsError
  - __Objective__: Confirms that GetAsync responds with an appropriate error when requested to retrieve a file that does not exist.
  - __Methodology__: Invokes GetAsync with a GUID for a non-existent file and examines the response for error indications.
  - __Verification__: Checks that Success is false and that the errors indicate the file could not be found.

* #### GetBytesAsync_ExtremelyLargeFile_ReturnsErrorOrHandlesGracefully
  - __Objective__: Assesses how GetBytesAsync deals with extremely large files, either by handling gracefully or returning an error.
  - __Methodology__: An exceedingly large file is created and GetBytesAsync is tasked with retrieving it. The method's ability to handle or report errors regarding the file size is observed.
  - __Verification__: Verifies either a successful operation (with correct file size) or the presence of expected error messages related to file size limitations.

* #### UpdateAsync_SimultaneousAccess_ReturnsTrue
  - __Objective__: Tests UpdateAsync's behavior under conditions of simultaneous access, ensuring it can successfully update a file.
  - __Methodology__: After creating a file, UpdateAsync is called to modify its contents. The operation's success is then evaluated.
  - __Verification__: Asserts the operation's success, indicating that UpdateAsync can handle simultaneous access scenarios effectively.

* #### GetBytesAsync_WithCancellation_ThrowsOperationCanceledException
  - __Objective__: Verifies that GetBytesAsync honors cancellation requests by aborting the operation and throwing OperationCanceledException.
  - __Methodology__: A cancellation token is set to the canceled state before calling GetBytesAsync. The operation's response to this cancellation is then tested.
  - __Verification__: Ensures that an OperationCanceledException is thrown, demonstrating proper cancellation behavior.

* #### ExistsAsync_FileExists_ReturnsTrue
  - __Objective__: Checks if ExistsAsync accurately determines the existence of a file.
  - __Methodology__: After creating a file, ExistsAsync is used to check for its presence based on its GUID.
  - __Verification__: Asserts that the result indicates the file exists, validating the method's ability to detect existing files.

* #### UpdateAsync_WithCancellation_ThrowsOperationCanceledException
  - __Objective__: Ensures that the UpdateAsync method properly responds to a cancellation request by aborting the update operation.
  - __Methodology__: Prepares a CancellationToken that is already cancelled and then invokes UpdateAsync with this token. The method should recognize the cancellation and halt the operation.
  - __Verification__: Confirms the method throws an OperationCanceledException, demonstrating it correctly responds to cancellation requests.

* #### UpdateAsync_SuccessfullyUpdatesFile_ReturnsSuccess
  - __Objective__: Verifies that UpdateAsync can successfully update the contents of an existing file.
  - __Methodology__: A file is initially created with predefined content. UpdateAsync is then used with new content for this file. Afterward, it verifies if the file content has been updated as expected.
  - __Verification__: Asserts the success of the operation and optionally checks if the file content matches the new content provided to UpdateAsync.

* #### ExistsAsync_WithCancellation_ThrowsOperationCanceledException
  - __Objective__: Tests whether the ExistsAsync method correctly handles a cancellation signal by throwing an OperationCanceledException.
  - __Methodology__: Invokes ExistsAsync with a CancellationToken that has been cancelled, expecting the method to abort and indicate the operation was cancelled.
  - __Verification__: Checks for an OperationCanceledException to be thrown, signifying the method's appropriate reaction to the cancellation request.

* #### StoreAsync_InvalidInput_ReturnsError
  - __Objective__: Checks how the StoreAsync method deals with invalid input, specifically null or empty streams.
  - __Methodology__: Calls StoreAsync with a StreamInfo object that has a null Stream property, simulating an invalid input scenario.
  - __Verification__: Asserts that the method returns a failure (Success is false) and contains error messages indicating the invalid input.

* #### GetAsync_WithCancellation_ThrowsOperationCanceledException
  - __Objective__: Ensures GetAsync respects cancellation requests by aborting the retrieval operation.
  - __Methodology__: A cancelled CancellationToken is passed to GetAsync. The operation is expected to recognize this cancellation and not proceed.
  - __Verification__: Confirms that an OperationCanceledException is thrown, indicating the operation was cancelled as requested.

* #### ExistsAsync_FileDoesNotExist_ReturnsFalse
  - __Objective__: Ensures ExistsAsync accurately reports the non-existence of a file.
  - __Methodology__: ExistsAsync is invoked with a GUID that does not match any existing file in the system. The method should then return a result indicating the file does not exist.
  - __Verification__: Verifies that the operation result indicates the file does not exist (ResultObject is false) and the operation itself is considered unsuccessful (Success is false).

* #### DeleteAsync_FileExists_DeletesFileSuccessfully
  - __Objective__: Tests the DeleteAsync method's ability to successfully delete an existing file.
  - __Methodology__: A file is first created, then DeleteAsync is called to delete it. The absence of the file is checked afterward.
  - __Verification__: Ensures the operation is successful and the file no longer exists, demonstrating effective file deletion capabilities.

* #### DeleteAsync_FileDoesNotExist_ReturnsError
  - __Objective__: Checks DeleteAsync's error handling when attempting to delete a file that doesn't exist.
  - __Methodology__: Attempts to delete a file using a GUID for a non-existent file and examines the response for appropriate error handling.
  - __Verification__: Asserts the operation was not successful (Success is false) and verifies the presence of error messages indicating the file could not be found.

* #### DeleteAsync_WithCancellation_ThrowsOperationCanceledException
  - __Objective__: Ensures DeleteAsync responds correctly to cancellation requests by aborting the delete operation.
  - __Methodology__: A CancellationToken is cancelled prior to invoking DeleteAsync. The method should halt and indicate the operation was cancelled.
  - __Verification__: Checks for an OperationCanceledException, validating the method's adherence to cancellation requests.

* #### GetHashAsync_FileExists_ComputesHashSuccessfully
  - __Objective__: Verifies that GetHashAsync can successfully compute and return the hash of an existing file.
  - __Methodology__: After creating a file with known content, GetHashAsync is used to compute its hash. The hash result is then compared to an expected value or simply checked for existence.
  - __Verification__: Asserts the operation's success and that the result object contains a valid hash string, confirming the method's functionality.

* #### GetHashAsync_FileDoesNotExist_ReturnsError
  - __Objective__: Tests GetHashAsync's response when attempting to compute the hash of a non-existent file.
  - __Methodology__: Invokes GetHashAsync with a GUID for a file that does not exist, expecting the
  - __Verification__: Ensures the operation returns a failure (Success is false) and the errors convey that the file could not be found, verifying appropriate error handling for absent files.

* #### GetHashAsync_WithCancellation_ThrowsOperationCanceledException
  - __Objective__: Confirms that GetHashAsync correctly aborts its process when a cancellation request is received, signifying responsive and controlled termination of the operation.
  - __Methodology__: A cancellation token that is already cancelled is passed to the method. GetHashAsync is expected to immediately recognize the cancellation and terminate its execution.
  - __Verification__: Asserts that an OperationCanceledException is thrown, proving the method's proper reaction to the cancellation signal, thus preventing unnecessary processing.

### Methodology and Verification Explained

#### Methodology
Each test is crafted to simulate specific scenarios that the FileShareLibrary methods might encounter during runtime. This includes handling valid cases (where operations should succeed), dealing with incorrect inputs or states (like non-existent files or invalid data), and responding to external signals such as cancellation requests. The tests make use of the Moq library to mock external dependencies, allowing for isolated testing of the library's logic without interference from the file system or other components.

#### Verification
* __Success Condition Checks__: Most tests verify the operation's success by inspecting the Success property of the returned OperationResult. Success here means the operation was completed as intended without encountering unforeseen issues.
* __Error Handling Checks__: For tests involving error scenarios, the verification involves ensuring the Success property is false and that the Errors collection within the OperationResult contains appropriate messages that accurately describe the encountered issue.
* __Exception Handling Checks__: For tests that involve operation cancellation, the expected outcome is the throwing of an OperationCanceledException. This exception indicates that the method respected the cancellation token and ceased its operation promptly.
