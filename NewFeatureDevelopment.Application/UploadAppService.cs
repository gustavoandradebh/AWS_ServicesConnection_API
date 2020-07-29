using NFD.Domain.Data;
using NFD.Domain.Exceptions;
using NFD.Domain.Interfaces;
using NFD.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NewFeatureDevelopment.Application
{
    public class UploadAppService : IUploadService
    {
        private readonly AmazonRDSContext _amazonRDS;
        private readonly IAwsS3Service _awsS3service;
        public UploadAppService(AmazonRDSContext amazonRDS, IAwsS3Service awsS3service)
        {
            _amazonRDS = amazonRDS;
            _awsS3service = awsS3service;
        }
        public async Task<ImageEntity> UploadFileAndDescription(ImageModel imageModel)
        {
            ValidateProperties(imageModel);

            var imageKey = await UploadImageToS3(imageModel);

            var imageEntity = await AddImageDetailsToRDS(imageModel, imageKey);
            
            return imageEntity;
        }

        private async Task<ImageEntity> AddImageDetailsToRDS(ImageModel imageModel, string imageKey)
        {
            try
            {
                var imageEntity = new ImageEntity
                {
                    Description = imageModel.Description,
                    FileSizeKb = ((decimal)imageModel.ImageFile.Length) / 1024,
                    FileType = imageModel.ImageFile.ContentType,
                    ImageKey = imageKey,
                    createdDate = DateTime.Now
                };

                _amazonRDS.Images.Add(imageEntity);
                var itensAdded = await _amazonRDS.SaveChangesAsync();
                if (itensAdded == 1)
                    return imageEntity;
                else
                    throw new CustomException(System.Net.HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again.");
            }
            catch
            {
                await _awsS3service.RemoveFileAsync(imageKey);
                throw new CustomException(System.Net.HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again.");
            }
        }

        private async Task<string> UploadImageToS3(ImageModel imageModel)
        {
            string imageKey = string.Empty;

            using (var fileStream = imageModel.ImageFile.OpenReadStream())
            {
                using (var ms = new MemoryStream())
                {
                    await fileStream.CopyToAsync(ms);
                    imageKey = await _awsS3service.UploadFileAsync(ms);
                }
            }

            return imageKey;
        }

        private static void ValidateProperties(ImageModel imageModel)
        {
            var imageFile = imageModel.ImageFile;

            if (imageFile == null || imageFile.Length == 0)
                throw new BadRequestException("Please select a valid image.");

            if (((decimal)imageFile.Length) / 1024 > 500)
                throw new BadRequestException("Please choose a file under 500kb.");

            string[] acceptedTypes = new string[] { "image/png", "image/jpeg" };
            if(!acceptedTypes.Contains(imageModel.ImageFile.ContentType.ToLower()))
                throw new BadRequestException("Please choose a file in PNG or JPEG type.");

            if (string.IsNullOrEmpty(imageModel.Description))
                throw new BadRequestException("Please provide a description.");
        }
    }
}
