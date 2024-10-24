using FileImportAPI.Data;
using FileImportAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FileImportAPI.Repositories
{
    public class ImportDataRepository : IImportDataRepository
    {
        private readonly ImportDataContext _context;

        public ImportDataRepository(ImportDataContext context)
        {
            _context = context;
        }

        public async Task BulkInsertAsync(IEnumerable<ImportData> data)
        {
            try
            {
                foreach (var record in data)
                {
                    // Check if the file is already being processed
                    var existingRecord = await _context.ImportDataRecords
                        .FirstOrDefaultAsync(r => r.FileName == record.FileName && r.IsProcessing);

                    if (existingRecord != null)
                    {
                        // Log and skip the record if it's already being processed
                        Console.WriteLine($"Skipping file {record.FileName}, it is already being processed.");
                        continue;
                    }

                    // Handle duplicate records based on file hash or other unique fields
                    var duplicateRecord = await _context.ImportDataRecords
                        .FirstOrDefaultAsync(r => r.FileName == record.FileName);

                    if (duplicateRecord != null)
                    {
                        // If a duplicate exists, update the existing record instead of inserting a new one
                        Console.WriteLine($"Updating existing file {record.FileName}.");
                        duplicateRecord.UserName = record.UserName;
                        duplicateRecord.Location = record.Location;
                        duplicateRecord.ImportedAt = record.ImportedAt;
                        duplicateRecord.IsProcessing = true;  // Mark it as processing
                        _context.ImportDataRecords.Update(duplicateRecord);
                    }
                    else
                    {
                        // If no duplicate, insert a new record
                        record.IsProcessing = true;
                        //record.Id = 0;  // Ensure new records don’t have IDs
                        await _context.ImportDataRecords.AddAsync(record);
                    }
                }

                await _context.SaveChangesAsync();  // Save changes to the database

                // Once processing is done, update the status to false for all records
                foreach (var record in data)
                {
                    var updatedRecord = await _context.ImportDataRecords
                        .FirstOrDefaultAsync(r => r.FileName == record.FileName);

                    if (updatedRecord != null)
                    {
                        updatedRecord.IsProcessing = false;
                    }
                }

                await _context.SaveChangesAsync();  // Save final status updates
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"BulkInsert Error: {ex.Message}");
                throw;
            }
        }

        // Get a file by its unique hash value
        public async Task<ImportData> GetFileByHashAsync(string fileHash)
        {
            return await _context.ImportDataRecords
                .FirstOrDefaultAsync(f => f.FileHash == fileHash);
        }

        // Get a record by its unique hash value
        public async Task<ImportData> GetRecordByHashAsync(string recordHash)
        {
            return await _context.ImportDataRecords
                .FirstOrDefaultAsync(r => r.RecordHash == recordHash);
        }

        // Get all records
        public async Task<List<ImportData>> GetAllAsync()
        {
            return await _context.ImportDataRecords.ToListAsync();
        }

        // Get a record by its ID
        public async Task<ImportData> GetByIdAsync(int id)
        {
            return await _context.ImportDataRecords.FindAsync(id);
        }

        // Get record by ID (implemented)
        public async Task<ImportData> GetRecordByIdAsync(int id)
        {
            return await _context.ImportDataRecords.FindAsync(id);
        }

        // Update an existing record
        public async Task UpdateRecordAsync(ImportData record)
        {
            try
            {
                _context.ImportDataRecords.Update(record);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating record: {ex.Message}");
                throw;
            }
        }
    }
}