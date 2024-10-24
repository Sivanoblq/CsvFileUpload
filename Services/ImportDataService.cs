using CsvHelper;
using FileImportAPI.Mapping;
using FileImportAPI.Models;
using FileImportAPI.Repositories;
using Microsoft.Extensions.Caching.Memory;  // Added for caching
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FileImportAPI.Services
{
    public class ImportDataService : IImportDataService
    {
        private readonly IImportDataRepository _repository;
        private readonly IMemoryCache _cache; // Added for caching
        private readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30); // Cache expiration time

        public ImportDataService(IImportDataRepository repository, IMemoryCache cache) // Cache injected via constructor
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task ProcessBulkFilesAsync(List<IFormFile> files)
        {
            foreach (var file in files)
            {
                try
                {
                    await ProcessFileAsync(file); // Handle each file individually
                }
                catch (Exception ex)
                {
                    // Log and continue processing remaining files
                    Console.WriteLine($"Failed to process the file '{file.FileName}'. Please check the file format or permissions. Error: {ex.Message}");
                }
            }
        }

        public async Task ProcessFileAsync(IFormFile file)
        {
            if (await IsFileAlreadyUploaded(file))
            {
                throw new Exception("This file has already been processed.");
            }

            try
            {
                // Read and validate CSV file
                using (var stream = new StreamReader(file.OpenReadStream()))
                {
                    using (var csv = new CsvReader(stream, CultureInfo.InvariantCulture))
                    {
                        csv.Context.RegisterClassMap<ImportDataMap>();
                        var records = csv.GetRecords<ImportData>().ToList();

                        if (records == null || !records.Any())
                        {
                            throw new Exception("No valid records found in the file.");
                        }

                        var fileHash = ComputeFileHash(file);
                        List<ImportData> validRecords = new List<ImportData>();

                        foreach (var record in records)
                        {
                            // Perform field validations
                            ValidateRecord(record);

                            // Compute record hash
                            var recordHash = ComputeRecordHash(record);
                            var cacheKey = $"RecordHash_{recordHash}";

                            // Check cache for existing record by hash
                            if (!_cache.TryGetValue(cacheKey, out ImportData existingRecord))
                            {
                                // If not found in cache, check repository
                                existingRecord = await _repository.GetRecordByHashAsync(recordHash);

                                // Cache the result
                                if (existingRecord != null)
                                {
                                    _cache.Set(cacheKey, existingRecord, CacheExpiration);
                                }
                            }

                            if (existingRecord != null)
                            {
                                // If the hash is the same, skip processing this record
                                Console.WriteLine($"Skipping duplicate record for FileName: {record.FileName}");
                                continue;
                            }

                            // Check if the record exists by ID and if it needs to be updated
                            var existingRecordById = await _repository.GetByIdAsync(record.Id);

                            if (existingRecordById != null)
                            {
                                if (existingRecordById.RecordHash != recordHash)
                                {
                                    // Update the record if it exists with a different hash
                                    existingRecordById.FileName = record.FileName;
                                    existingRecordById.ImportedAt = record.ImportedAt;
                                    existingRecordById.RecordHash = recordHash;
                                    existingRecordById.FileHash = fileHash;
                                    existingRecordById.IsProcessing = false;

                                    await _repository.UpdateRecordAsync(existingRecordById);
                                    Console.WriteLine($"Record {record.Id} updated successfully.");
                                }
                                else
                                {
                                    Console.WriteLine($"Record {record.Id} already up to date, no changes needed.");
                                }
                            }
                            else
                            {
                                // If it's a new record, insert it
                                record.FileHash = fileHash;
                                record.RecordHash = recordHash;
                                record.IsProcessing = false;

                                validRecords.Add(record);
                                Console.WriteLine($"New record {record.Id} added for FileName: {record.FileName}");
                            }
                        }

                        // Save valid records to the database (new ones)
                        if (validRecords.Any())
                        {
                            await _repository.BulkInsertAsync(validRecords);
                            // Invalidate cache since new data has been added
                            _cache.Remove("AllImportData");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {file.FileName}: {ex.Message}");
                throw new Exception($"Error processing file {file.FileName}: {ex.Message}");
            }
        }

        public async Task<bool> IsFileAlreadyUploaded(IFormFile file)
        {
            var fileHash = ComputeFileHash(file);
            var cacheKey = $"FileHash_{fileHash}";

            if (!_cache.TryGetValue(cacheKey, out ImportData existingFile))
            {
                existingFile = await _repository.GetFileByHashAsync(fileHash);

                if (existingFile != null)
                {
                    _cache.Set(cacheKey, existingFile, CacheExpiration);
                }
            }

            return existingFile != null && !HasFileChanged(file, existingFile);
        }

        private bool HasFileChanged(IFormFile newFile, ImportData existingFile)
        {
            // Logic to determine if the file has changed based on some metadata or record-level comparison
            return false;
        }

        private string ComputeFileHash(IFormFile file)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = file.OpenReadStream())
                {
                    var hashBytes = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private string ComputeRecordHash(ImportData record)
        {
            using (var sha256 = SHA256.Create())
            {
                var rawData = $"{record.FileName}{record.ImportedAt:o}";
                var bytes = Encoding.UTF8.GetBytes(rawData);
                var hashBytes = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private void ValidateRecord(ImportData record)
        {
            if (record.Id <= 0)
            {
                throw new Exception($"Invalid value for 'Id' in record with FileName: {record.FileName}. Must be a positive integer.");
            }

            if (string.IsNullOrWhiteSpace(record.FileName))
            {
                throw new Exception("FileName cannot be empty.");
            }

            if (record.ImportedAt == default)
            {
                throw new Exception($"Invalid date in record with FileName: {record.FileName}.");
            }
        }
    }
}
