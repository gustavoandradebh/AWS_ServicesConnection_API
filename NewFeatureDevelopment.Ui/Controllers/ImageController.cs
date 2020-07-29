using Microsoft.AspNetCore.Mvc;
using NFD.Domain.Data;
using NFD.Domain.Interfaces;
using System.Threading.Tasks;

namespace NFD.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IUploadService _uploadService;
        private readonly IElasticsearchService _elasticsearchService;
        public ImageController(IUploadService uploadService, IElasticsearchService elasticsearchService)
        {
            _uploadService = uploadService;
            _elasticsearchService = elasticsearchService;
        }

        [HttpPost]
        public async Task<ActionResult> UploadImage([FromForm] ImageModel imageModel)
        {
            var imageEntity = await _uploadService.UploadFileAndDescription(imageModel);
            return Created("/api/image/", imageEntity);
        }

        [HttpPost("migrate")]
        public async Task<ActionResult> MigrateRdsToElasticsearch()
        {
            await _elasticsearchService.MigrateDataRDSAsync();

            return Ok();
        }

        [HttpGet]
        public async Task<ActionResult> SearchImagesOnElasticSearch([FromQuery] string description, [FromQuery] string filetype, [FromQuery] string filesize, [FromQuery] string page)
        {
            var result = await _elasticsearchService.SearchImagesAsync(description, filetype, filesize, page);

            if (result == null || result.Count == 0)
                return NoContent();
            else
                return Ok(result);
        }
    }
}
