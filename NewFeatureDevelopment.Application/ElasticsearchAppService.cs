using Microsoft.EntityFrameworkCore.Internal;
using NFD.Domain.Data;
using NFD.Domain.Interfaces;
using NFD.Infrastructure;
using NFD.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NFD.Application
{
    public class ElasticsearchAppService : IElasticsearchService
    {
        private readonly IElasticsearchClient _elasticsearchClient;
        private readonly AmazonRDSContext _amazonRDS;

        private const int PAGE_SIZE = 20;
        public ElasticsearchAppService(IElasticsearchClient elasticsearchClient, AmazonRDSContext amazonRDS)
        {
            _elasticsearchClient = elasticsearchClient;
            _amazonRDS = amazonRDS;
        }

        public async Task<bool> MigrateDataRDSAsync()
        {
            var allImagesDetails = _amazonRDS.Images;

            foreach (var imageDetails in allImagesDetails)
            {
                var imageModel = new ImageDetail
                {
                    Description = imageDetails.Description,
                    FileSizeKb = String.Format(CultureInfo.GetCultureInfo("en-US"), "{0:0.##}", imageDetails.FileSizeKb),
                    FileType = imageDetails.FileType.Replace("image/", "")
                };
                await _elasticsearchClient.InsertDataOnElasticsearch(imageDetails.Id, imageModel);
            };

            return true;
        }

        public async Task<List<ImageDetail>> SearchImagesAsync(string description, string filetype, string filesize, string initialPage)
        {
            if (string.IsNullOrWhiteSpace(initialPage))
                initialPage = "0";

            var imagesFound = new List<ImageDetail>();

            var listQuery = new List<string>();
            if (!string.IsNullOrWhiteSpace(description))
                listQuery.Add($"Description:*{description}*");

            if (!string.IsNullOrWhiteSpace(filetype))
                listQuery.Add($"FileType:{filetype}");

            if (!string.IsNullOrWhiteSpace(filesize))
                listQuery.Add($"FileSizeKb:{filesize}");

            string queryStr = null;

            if (listQuery.Any())
                queryStr = string.Join(" AND ", listQuery);

            int fromItem = Convert.ToInt32(initialPage) * PAGE_SIZE;

            var result = await _elasticsearchClient.QueryImagesOnElasticsearch(queryStr, PAGE_SIZE, fromItem, "true");

            if (result.hits.hits.Any())
            {
                foreach (var item in result.hits.hits)
                {
                    imagesFound.Add(
                        new ImageDetail
                        {
                            Description = item._source.Description,
                            FileSizeKb = item._source.FileSizeKb,
                            FileType = $"image/{item._source.FileType}"
                        }
                    );
                }
            }

            return imagesFound;
        }
    }
}
