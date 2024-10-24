using FileImportAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FileImportAPI.Data
{
    public class ImportDataContext : DbContext
    {
        public ImportDataContext(DbContextOptions<ImportDataContext> options) : base(options) { }

        public DbSet<ImportData> ImportDataRecords { get; set; }
    }
}