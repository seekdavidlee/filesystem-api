using FileSystemApi.Models;
using FileSystemApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FileSystemApi.Controllers;

[Route("storage/files")]
[ApiController]
public class StorageFilesController(ILogger<StorageFilesController> logger, DbProvider provider) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string path)
    {
        var server = provider.Database.Multiplexer.GetServer(provider.Database.Multiplexer.GetEndPoints().First());

        List<string> keys = [];
        string pattern = $"{path}*";
        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            keys.Add(key.ToString());
        }

        logger.LogInformation("found {keys} keys for path: {path}", keys.Count, path);
        return Ok(keys ?? []);
    }

    [HttpPut("object")]
    public async Task<IActionResult> Add([FromQuery] string path, CancellationToken cancellationToken)
    {
        var storageLocation = Environment.GetEnvironmentVariable("STORAGE_LOCATION");
        if (string.IsNullOrEmpty(storageLocation))
        {
            logger.LogError("STORAGE_LOCATION is not set");
            return BadRequest("STORAGE_LOCATION is not set");
        }

        using var ms = new StreamReader(Request.Body);
        var fileContent = await ms.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrEmpty(fileContent))
        {
            logger.LogError("empty file content is invalid");
            return BadRequest("empty file content is invalid");
        }

        var fileId = Guid.NewGuid().ToString("N");
        await System.IO.File.WriteAllTextAsync($"{storageLocation}/{fileId}", fileContent, cancellationToken);
        await provider.Database.StringSetAsync(path, fileId);
        await System.IO.File.WriteAllTextAsync($"{storageLocation}/{fileId}.info.json", JsonSerializer.Serialize(new StorageFileInfo(path)), cancellationToken);
        return Accepted();
    }

    [HttpGet("object")]
    public async Task<IActionResult> Get([FromQuery] string path, CancellationToken cancellationToken)
    {
        var storageLocation = Environment.GetEnvironmentVariable("STORAGE_LOCATION");
        if (string.IsNullOrEmpty(storageLocation))
        {
            logger.LogError("STORAGE_LOCATION is not set");
            return BadRequest("STORAGE_LOCATION is not set");
        }

        var result = await provider.Database.StringGetAsync(path);

        if (!result.HasValue || result.IsNullOrEmpty)
        {
            logger.LogWarning("key {path} contains empty value", path);
            return NotFound();
        }
        string filePath = $"{storageLocation}/{result}";
        if (!System.IO.File.Exists(filePath))
        {
            logger.LogWarning("missing file {path}", filePath);
            return NotFound();
        }

        return Ok(await System.IO.File.ReadAllTextAsync(filePath, cancellationToken));
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] string path)
    {
        var storageLocation = Environment.GetEnvironmentVariable("STORAGE_LOCATION");
        if (string.IsNullOrEmpty(storageLocation))
        {
            logger.LogError("STORAGE_LOCATION is not set");
            return BadRequest("STORAGE_LOCATION is not set");
        }

        var server = provider.Database.Multiplexer.GetServer(provider.Database.Multiplexer.GetEndPoints().First());

        List<string> keys = [];
        string pattern = $"{path}*";
        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            keys.Add(key.ToString());
        }

        logger.LogInformation("found {keys} keys for path: {path} to delete", keys.Count, path);

        foreach (var key in keys)
        {
            var result = await provider.Database.StringGetAsync(key);
            if (result.HasValue && !result.IsNullOrEmpty)
            {
                string filePath = $"{storageLocation}/{result}";
                if (System.IO.File.Exists(filePath))
                {
                    logger.LogInformation("deleting file {path}", filePath);
                    System.IO.File.Delete(filePath);
                }

                var fileInfoPath = $"{filePath}.info.json";
                if (System.IO.File.Exists(fileInfoPath))
                {
                    logger.LogInformation("deleting file info {path}", fileInfoPath);
                    System.IO.File.Delete(fileInfoPath);
                }
            }
            await provider.Database.KeyDeleteAsync(key);
            logger.LogInformation("deleting key {path}", key);
        }

        return NoContent();
    }
}
