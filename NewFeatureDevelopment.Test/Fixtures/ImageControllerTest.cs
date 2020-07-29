using Amazon.S3;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NewFeatureDevelopment.Application;
using NFD.Application;
using NFD.Domain.Data;
using NFD.Domain.Exceptions;
using NFD.Domain.Interfaces;
using NFD.Infrastructure;
using NFD.Infrastructure.Interfaces;
using NFD.Test.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static NFD.Domain.Data.ElasticsearchQueryResult;

namespace NFD.Test.Fixtures
{
    public class ImageControllerTest
    {
        private const string locationUri = "/api/image/";

        protected DbContextOptions<AmazonRDSContext> contextOptions { get; }
        private readonly Mock<IAwsS3Service> _awsServiceMock;
        private readonly Mock<IElasticsearchService> _elasticsearchServiceMock;
        private readonly Mock<IElasticsearchClient> _elasticsearchClientMock;
        private readonly Mock<IAmazonS3> _s3ClientMock;

        public ImageControllerTest()
        {
            this.contextOptions = new DbContextOptionsBuilder<AmazonRDSContext>()
                .UseInMemoryDatabase("TestDB")
                .Options;

            _awsServiceMock = new Mock<IAwsS3Service>();
            _elasticsearchServiceMock = new Mock<IElasticsearchService>();
            _elasticsearchClientMock = new Mock<IElasticsearchClient>();
            _s3ClientMock = new Mock<IAmazonS3>();
            Seed();
        }

        [Fact]
        public async void PostImage_FullPayload_201_WithLocation()
        {
            using (var context = new AmazonRDSContext(contextOptions))
            {
                var _uploadService = new UploadAppService(context, _awsServiceMock.Object);

                var imageModel = new ImageModel
                {
                    Description = "Test description",
                    ImageFile = CreateFakeFormFile(100, "image.jpg")
                };

                var controller = Util.ConfigController(_uploadService, context, _awsServiceMock.Object, _elasticsearchServiceMock.Object);

                var key = Guid.NewGuid().ToString();
                _awsServiceMock.Setup(x => x.UploadFileAsync(It.IsAny<Stream>()))
                    .Returns(Task.FromResult(key));

                var result = await controller.UploadImage(imageModel);

                var imageEntity = GetCreatedObject<ImageEntity>(result);

                Assert.True(imageEntity.Id > 0);
                Assert.True(((CreatedResult)result).StatusCode.Value == (int)HttpStatusCode.Created);
                Assert.True(((CreatedResult)result).Location == locationUri);
            }
        }

        [Fact]
        public void PostImage_WithoutDescription_400()
        {
            using (var context = new AmazonRDSContext(contextOptions))
            {
                var _uploadService = new UploadAppService(context, _awsServiceMock.Object);

                var imageModel = new ImageModel
                {
                    Description = "",
                    ImageFile = CreateFakeFormFile(100, "image.png")
                };

                var controller = Util.ConfigController(_uploadService, context, _awsServiceMock.Object, _elasticsearchServiceMock.Object);

                var key = Guid.NewGuid().ToString();
                _awsServiceMock.Setup(x => x.UploadFileAsync(It.IsAny<Stream>()))
                    .Returns(Task.FromResult(key));

                Func<Task> action = async () => await controller.UploadImage(imageModel);

                action.Should().Throw<BadRequestException>();
            }
        }

        [Fact]
        public void PostImage_WithoutFile_400()
        {
            using (var context = new AmazonRDSContext(contextOptions))
            {
                var _uploadService = new UploadAppService(context, _awsServiceMock.Object);

                var imageModel = new ImageModel
                {
                    Description = "Test",
                    ImageFile = null
                };

                var controller = Util.ConfigController(_uploadService, context, _awsServiceMock.Object, _elasticsearchServiceMock.Object);

                var key = Guid.NewGuid().ToString();
                _awsServiceMock.Setup(x => x.UploadFileAsync(It.IsAny<Stream>()))
                    .Returns(Task.FromResult(key));

                Func<Task> action = async () => await controller.UploadImage(imageModel);

                action.Should().Throw<BadRequestException>();
            }
        }

        [Fact]
        public void PostImage_ImageBiggerThan500kb_400()
        {
            using (var context = new AmazonRDSContext(contextOptions))
            {
                var _uploadService = new UploadAppService(context, _awsServiceMock.Object);

                var imageModel = new ImageModel
                {
                    Description = "Test",
                    ImageFile = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("dummy image")), 0, 550000, "Data", "image.png")
                };

                var controller = Util.ConfigController(_uploadService, context, _awsServiceMock.Object, _elasticsearchServiceMock.Object);

                var key = Guid.NewGuid().ToString();
                _awsServiceMock.Setup(x => x.UploadFileAsync(It.IsAny<Stream>()))
                    .Returns(Task.FromResult(key));

                Func<Task> action = async () => await controller.UploadImage(imageModel);

                action.Should().Throw<BadRequestException>();
            }
        }

        [Fact]
        public void PostImage_ImageNotJpgPng_400()
        {
            using (var context = new AmazonRDSContext(contextOptions))
            {
                var _uploadService = new UploadAppService(context, _awsServiceMock.Object);

                var imageModel = new ImageModel
                {
                    Description = "Test",
                    ImageFile = CreateFakeFormFile(1100, "image.zip", "application/zip")
                };

                var controller = Util.ConfigController(_uploadService, context, _awsServiceMock.Object, _elasticsearchServiceMock.Object);

                var key = Guid.NewGuid().ToString();
                _awsServiceMock.Setup(x => x.UploadFileAsync(It.IsAny<Stream>()))
                    .Returns(Task.FromResult(key));

                Func<Task> action = async () => await controller.UploadImage(imageModel);

                action.Should().Throw<BadRequestException>();
            }
        }

        [Fact]
        public void PostImage_EmptyPayload_400()
        {
            using (var context = new AmazonRDSContext(contextOptions))
            {
                var _uploadService = new UploadAppService(context, _awsServiceMock.Object);

                var imageModel = new ImageModel
                {
                    Description = null,
                    ImageFile = null
                };

                var controller = Util.ConfigController(_uploadService, context, _awsServiceMock.Object, _elasticsearchServiceMock.Object);

                var key = Guid.NewGuid().ToString();
                _awsServiceMock.Setup(x => x.UploadFileAsync(It.IsAny<Stream>()))
                    .Returns(Task.FromResult(key));

                Func<Task> action = async () => await controller.UploadImage(imageModel);

                action.Should().Throw<BadRequestException>();
            }
        }

        [Fact]
        public async void MigrateRdsToElasticsearch_200()
        {
            using (var context = new AmazonRDSContext(contextOptions))
            {
                var _elasticsearchAppService = new ElasticsearchAppService(_elasticsearchClientMock.Object, context);
                var _uploadService = new UploadAppService(context, _awsServiceMock.Object);

                var imageEntity = new ImageEntity
                {
                    Description = "Test description",
                    FileSizeKb = 200,
                    FileType = "image/png",
                    ImageKey = "Test",
                    createdDate = DateTime.Now
                };
                context.Images.Add(imageEntity);
                context.SaveChanges();

                _elasticsearchClientMock.Setup(x => x.InsertDataOnElasticsearch(It.IsAny<long>(), It.IsAny<ImageDetail>()))
                    .Returns(Task.FromResult(new ElasticsearchPutResponse()));

                var controller = Util.ConfigController(_uploadService, context, _awsServiceMock.Object, _elasticsearchAppService);

                var result = await controller.MigrateRdsToElasticsearch();

                var okResult = GetOkObject(result);

                Assert.True(okResult.StatusCode == (int)HttpStatusCode.OK);
            }
        }

        [Fact]
        public async void SearchImagesOnElasticSearch_AllQueryParams_200()
        {
            using (var context = new AmazonRDSContext(contextOptions))
            {
                var _elasticsearchAppService = new ElasticsearchAppService(_elasticsearchClientMock.Object, context);
                var _uploadService = new UploadAppService(context, _awsServiceMock.Object);

                var controller = Util.ConfigController(_uploadService, context, _awsServiceMock.Object, _elasticsearchAppService);

                var hitsDetails = new List<Hit>
                {
                    new Hit { _source = new ImageDetail { Description = "test", FileSizeKb = "120", FileType = "image/jpeg" } }
                };

                var elasticsearchQueryResult = new ElasticsearchQueryResult
                {
                    hits = new Hits { hits = hitsDetails }
                };

                _elasticsearchClientMock.Setup(x => x.QueryImagesOnElasticsearch(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                    .Returns(Task.FromResult(elasticsearchQueryResult));

                var result = await controller.SearchImagesOnElasticSearch("test", "jpeg", "120", "0");

                Assert.True(((List<ImageDetail>)((OkObjectResult)result).Value).Count > 0);
                Assert.True(((OkObjectResult)result).StatusCode == (int)HttpStatusCode.OK);
            }
        }

        [Fact]
        public async void SearchImagesOnElasticSearch_NullQueryParams_200()
        {
            using (var context = new AmazonRDSContext(contextOptions))
            {
                var _elasticsearchAppService = new ElasticsearchAppService(_elasticsearchClientMock.Object, context);
                var _uploadService = new UploadAppService(context, _awsServiceMock.Object);

                var controller = Util.ConfigController(_uploadService, context, _awsServiceMock.Object, _elasticsearchAppService);

                var hitsDetails = new List<Hit> {
                    new Hit { _source = new ImageDetail { Description = "test", FileSizeKb = "120", FileType = "image/jpeg" } }
                };

                var elasticsearchQueryResult = new ElasticsearchQueryResult {
                    hits = new Hits { hits = hitsDetails }
                };

                _elasticsearchClientMock.Setup(x => x.QueryImagesOnElasticsearch(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                    .Returns(Task.FromResult(elasticsearchQueryResult));

                var result = await controller.SearchImagesOnElasticSearch(null, null, null, null);

                Assert.True(((List<ImageDetail>)((OkObjectResult)result).Value).Count > 0);
                Assert.True(((OkObjectResult)result).StatusCode == (int)HttpStatusCode.OK);
            }
        }

        [Fact]
        public async void SearchImagesOnElasticSearch_AllQueryParams_204()
        {
            using (var context = new AmazonRDSContext(contextOptions))
            {
                var _elasticsearchAppService = new ElasticsearchAppService(_elasticsearchClientMock.Object, context);
                var _uploadService = new UploadAppService(context, _awsServiceMock.Object);

                var controller = Util.ConfigController(_uploadService, context, _awsServiceMock.Object, _elasticsearchAppService);

                var hitsDetails = new List<Hit>();

                var elasticsearchQueryResult = new ElasticsearchQueryResult
                {
                    hits = new Hits { hits = hitsDetails }
                };

                _elasticsearchClientMock.Setup(x => x.QueryImagesOnElasticsearch(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                    .Returns(Task.FromResult(elasticsearchQueryResult));

                var result = await controller.SearchImagesOnElasticSearch("test", "jpeg", "120", "0");

                Assert.True(((NoContentResult)result).StatusCode == (int)HttpStatusCode.NoContent);
            }
        }

        [Fact]
        public async void UploadFileAsync_ReturnKey()
        {
            AwsS3Service awsS3Service = new AwsS3Service(_s3ClientMock.Object);

            var result = await awsS3Service.UploadFileAsync(new MemoryStream(Encoding.UTF8.GetBytes("dummy image")));

            Assert.True(!string.IsNullOrEmpty(result));
        }

        [Fact]
        public async void RemoveFileAsync_ReturnTrue()
        {
            AwsS3Service awsS3Service = new AwsS3Service(_s3ClientMock.Object);

            var result = await awsS3Service.RemoveFileAsync("DummyKey");

            Assert.True(result);

        }

        private static FormFile CreateFakeFormFile(int size, string name, string contenttype = "image/jpeg")
        {
            return new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("dummy image")), 0, size, "Data", name)
            {
                Headers = new HeaderDictionary(),
                ContentType = contenttype
            };
        }

        private T GetCreatedObject<T>(IActionResult result)
        {
            var createdObjectResult = (CreatedResult)result;
            return (T)createdObjectResult.Value;
        }

        private OkResult GetOkObject(IActionResult result)
        {
            var okObjectResult = (OkResult)result;
            return okObjectResult;
        }

        private void Seed()
        {
            using (var _apiContext = new AmazonRDSContext(contextOptions))
            {
                _apiContext.Database.EnsureDeleted();
                _apiContext.Database.EnsureCreated();

                _apiContext.Images.SingleOrDefault();
            }
        }
    }
}
