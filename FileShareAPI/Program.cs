using FileShareLibrary;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
	serverOptions.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
});

var fileSharePath = builder.Configuration.GetValue<string>("FileSharePath");
builder.Services.AddSingleton<IContentProvider<StringKey>>(serviceProvider =>
{
	var logger = serviceProvider.GetRequiredService<ILogger<FileShareContentProvider>>();
	return new FileShareContentProvider(fileSharePath, logger);
});

builder.Services.AddLogging(configure => configure.AddConsole());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/upload", async (IFormFile file, IContentProvider<StringKey> provider, ILogger<FileShareContentProvider> logger) =>
{
	var fileName = Path.GetFileName(file.FileName); // Use the uploaded file's name directly
	using var stream = file.OpenReadStream();
	var streamInfo = new StreamInfo { Stream = stream, Length = stream.Length };
	var result = await provider.StoreAsync(fileName, streamInfo, CancellationToken.None);
	return result.Success ? Results.Ok($"File uploaded: {fileName}") : Results.BadRequest(result.Errors);
}).AllowAnonymous().DisableAntiforgery();

app.MapGet("/download/{fileName}", async (string fileName, IContentProvider<StringKey> provider) =>
{
	var result = await provider.GetAsync(fileName, CancellationToken.None);
	if (!result.Success)
	{
		return Results.NotFound("File not found.");
	}
	return Results.File(result.ResultObject.Stream, "application/octet-stream", fileName);
}).AllowAnonymous().DisableAntiforgery();

app.MapDelete("/delete/{fileName}", async (string fileName, IContentProvider<StringKey> provider) =>
{
	var result = await provider.DeleteAsync(fileName, CancellationToken.None);
	return result.Success ? Results.Ok($"File deleted: {fileName}") : Results.NotFound("File not found.");
}).AllowAnonymous().DisableAntiforgery();

app.MapGet("/exists/{fileName}", async (string fileName, IContentProvider<StringKey> provider) =>
{
	var result = await provider.ExistsAsync(fileName, CancellationToken.None);
	return result.ResultObject ? Results.Ok($"File exists: {fileName}") : Results.NotFound("File not found.");
}).AllowAnonymous().DisableAntiforgery();

app.MapPut("/update/{fileName}", async (string fileName, IFormFile file, IContentProvider<StringKey> provider, ILogger<FileShareContentProvider> logger) =>
{
	using var stream = file.OpenReadStream();
	var streamInfo = new StreamInfo { Stream = stream, Length = stream.Length };
	var result = await provider.UpdateAsync(fileName, streamInfo, CancellationToken.None);
	return result.Success ? Results.Ok($"File updated: {fileName}") : Results.BadRequest(result.Errors);
}).AllowAnonymous().DisableAntiforgery();

app.MapGet("/bytes/{fileName}", async (string fileName, IContentProvider<StringKey> provider) =>
{
	var result = await provider.GetBytesAsync(fileName, CancellationToken.None);
	if (!result.Success)
	{
		return Results.NotFound("File not found.");
	}
	return Results.Ok(result.ResultObject);
}).AllowAnonymous().DisableAntiforgery();

app.MapGet("/hash/{fileName}", async (string fileName, IContentProvider<StringKey> provider) =>
{
	var result = await provider.GetHashAsync(fileName, CancellationToken.None);
	if (!result.Success)
	{
		return Results.NotFound("File not found.");
	}
	return Results.Ok(result.ResultObject);
}).AllowAnonymous().DisableAntiforgery();

app.Run();
