using NFD.Domain.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NFD.Domain.Interfaces
{
    public interface IElasticsearchService
    {
        Task<bool> MigrateDataRDSAsync();

        Task<List<ImageDetail>> SearchImagesAsync(string description, string filetype, string filesize, string initialPage);
    }
}
