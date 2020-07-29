using NFD.Domain.Data;
using RestEase;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NFD.Infrastructure.Interfaces
{

    public interface IElasticsearchClient
    {
        [Header("Authorization")]
        AuthenticationHeaderValue Authorization { get; set; }

        [Put("images/_doc/{id}")]
        Task<ElasticsearchPutResponse> InsertDataOnElasticsearch([Path] long id, [Body] ImageDetail imageDetails);

        [Get("images/_search/")]
        Task<ElasticsearchQueryResult> QueryImagesOnElasticsearch([Query] string q, [Query] int size, [Query] int from, [Query] string pretty);
    }
}
