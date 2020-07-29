using NFD.Domain.Data;
using System.Threading.Tasks;

namespace NFD.Domain.Interfaces
{
    public interface IUploadService
    {
        Task<ImageEntity> UploadFileAndDescription(ImageModel imageModel);
    }
}
