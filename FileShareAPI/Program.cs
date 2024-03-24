using FileShareLibrary;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
	serverOptions.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
});

var fileSharePath = builder.Configuration.GetValue<string>("FileSharePath");
builder.Services.AddSingleton<IContentProvider<Guid>>(serviceProvider =>
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

app.MapPost("/upload", async (IFormFile file, IContentProvider<Guid> provider, ILogger<FileShareContentProvider> logger) =>
{
	var fileId = Guid.NewGuid();
	using var stream = file.OpenReadStream();
	var streamInfo = new StreamInfo { Stream = stream, Length = stream.Length };
	var result = await provider.StoreAsync(fileId, streamInfo, CancellationToken.None);
	return result.Success ? Results.Ok($"File uploaded with ID: {fileId}") : Results.BadRequest(result.Errors);
}).AllowAnonymous().DisableAntiforgery(); 

app.MapGet("/download/{fileId}", async (Guid fileId, IContentProvider<Guid> provider) =>
{
	var result = await provider.GetAsync(fileId, CancellationToken.None);
	if (!result.Success)
	{
		return Results.NotFound("File not found.");
	}
	return Results.File(result.ResultObject.Stream, "application/octet-stream", $"{fileId}");
}).AllowAnonymous().DisableAntiforgery(); 

app.MapDelete("/delete/{fileId}", async (Guid fileId, IContentProvider<Guid> provider) =>
{
	var result = await provider.DeleteAsync(fileId, CancellationToken.None);
	return result.Success ? Results.Ok("File deleted.") : Results.NotFound("File not found.");
}).AllowAnonymous().DisableAntiforgery();


app.MapGet("/exists/{fileId}", async (Guid fileId, IContentProvider<Guid> provider) =>
{
	var result = await provider.ExistsAsync(fileId, CancellationToken.None);
	return result.ResultObject ? Results.Ok("File exists.") : Results.NotFound("File not found.");
}).AllowAnonymous().DisableAntiforgery();

app.MapPut("/update/{fileId}", async (Guid fileId, IFormFile file, IContentProvider<Guid> provider, ILogger<FileShareContentProvider> logger) =>
{
	using var stream = file.OpenReadStream();
	var streamInfo = new StreamInfo { Stream = stream, Length = stream.Length };
	var result = await provider.UpdateAsync(fileId, streamInfo, CancellationToken.None);
	return result.Success ? Results.Ok($"File {fileId} updated.") : Results.BadRequest(result.Errors);
}).AllowAnonymous().DisableAntiforgery();

app.MapGet("/bytes/{fileId}", async (Guid fileId, IContentProvider<Guid> provider) =>
{
	var result = await provider.GetBytesAsync(fileId, CancellationToken.None);
	if (!result.Success)
	{
		return Results.NotFound("File not found.");
	}
	return Results.Ok(result.ResultObject);
}).AllowAnonymous().DisableAntiforgery();

app.MapGet("/hash/{fileId}", async (Guid fileId, IContentProvider<Guid> provider) =>
{
	var result = await provider.GetHashAsync(fileId, CancellationToken.None);
	if (!result.Success)
	{
		return Results.NotFound("File not found.");
	}
	return Results.Ok(result.ResultObject);
}).AllowAnonymous().DisableAntiforgery();

app.Run();
