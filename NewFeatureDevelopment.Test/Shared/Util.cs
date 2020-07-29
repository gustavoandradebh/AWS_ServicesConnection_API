using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewFeatureDevelopment.Application;
using NFD.Domain.Interfaces;
using NFD.Infrastructure;
using NFD.Ui.Controllers;
using System.IO;

namespace NFD.Test.Shared
{
    public static class Util
    {
        public static HttpContext ConfigRequest(string data)
        {
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(data));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = stream;
            httpContext.Request.ContentLength = stream.Length;

            return httpContext;
        }

        public static ImageController ConfigController(IUploadService uploadService, AmazonRDSContext amazonRDSContext, IAwsS3Service awsS3service, IElasticsearchService elasticsearchService)
        {
            var service = new UploadAppService(amazonRDSContext, awsS3service);

            var controllerContext = new ControllerContext()
            {
                HttpContext = ConfigRequest("dummy data"),
            };

            var controller = new ImageController(uploadService, elasticsearchService)
            {
                ControllerContext = controllerContext
            };

            return controller;
        }
    }
}
