using FileImportAPI.Models;

namespace FileImportAPI.Services
{
    public interface IImportDataService
    {
        Task ProcessFileAsync(IFormFile file);
        Task ProcessBulkFilesAsync(List<IFormFile> files);
        
    }
}
