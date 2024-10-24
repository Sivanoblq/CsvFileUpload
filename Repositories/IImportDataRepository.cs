using FileImportAPI.Models;

namespace FileImportAPI.Repositories
{
    public interface IImportDataRepository
    {
        Task BulkInsertAsync(IEnumerable<ImportData> data);
        Task<ImportData> GetRecordByIdAsync(int id); // Changed to match your service code
        Task<List<ImportData>> GetAllAsync();
        Task<ImportData> GetFileByHashAsync(string fileHash);
        Task<ImportData> GetRecordByHashAsync(string recordHash);
        Task UpdateRecordAsync(ImportData record); // Added this method for updating records
        Task<ImportData> GetByIdAsync(int id);
    }
}
