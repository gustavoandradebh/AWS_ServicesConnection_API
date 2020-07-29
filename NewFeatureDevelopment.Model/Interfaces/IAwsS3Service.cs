using System.IO;
using System.Threading.Tasks;

namespace NFD.Domain.Interfaces
{
    public interface IAwsS3Service
    {
        Task<string> UploadFileAsync(Stream fileStream);
        Task<bool> RemoveFileAsync(string fileName);
    }
}
