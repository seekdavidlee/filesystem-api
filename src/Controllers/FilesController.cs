using FileSystemApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileSystemApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FilesController(ILogger<FilesController> logger, DbProvider provider) : ControllerBase
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
        public async Task<IActionResult> Add([FromQuery] string path)
        {
            using var ms = new StreamReader(Request.Body);
            var fileContent = await ms.ReadToEndAsync();
            await provider.Database.StringSetAsync(path, fileContent.ToString());
            return Accepted();
        }

        [HttpGet("object")]
        public async Task<IActionResult> Get([FromQuery] string path)
        {
            var result = await provider.Database.StringGetAsync(path);
            return Ok(result.ToString());
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery] string path)
        {
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
                await provider.Database.KeyDeleteAsync(key);
                logger.LogInformation("deleting {path}", key);
            }

            return NoContent();
        }
    }
}
