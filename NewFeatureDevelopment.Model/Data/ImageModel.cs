using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace NFD.Domain.Data
{
    public class ImageModel
    {
        [Required]
        public string Description { get; set; }

        public IFormFile ImageFile { get; set; }
    }
}
