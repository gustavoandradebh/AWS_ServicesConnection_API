using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace NFD.Domain.Data
{
    [ExcludeFromCodeCoverage]
    [Table("Images")]
    public class ImageEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        
        [Required]
        public string Description { get; set; }

        [Required]
        public string FileType { get; set; }

        [Required]
        public decimal FileSizeKb { get; set; }

        [Required]
        public string ImageKey { get; set; }

        [Required]
        public DateTime createdDate { get; set; }
    }
}
