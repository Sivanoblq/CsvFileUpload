using CsvHelper.Configuration;
using FileImportAPI.Models;

namespace FileImportAPI.Mapping
{
    public class ImportDataMap : ClassMap<ImportData>
    {
        public ImportDataMap()
        {
            Map(m => m.Id).Name("Id");
            Map(m => m.FileName).Name("FileName");
            Map(m => m.UserName).Name("UserName");
            Map(m => m.Location).Name("Location");
            Map(m => m.ImportedAt).Name("ImportedAt");

            // Ignore the IsProcessing field since it's not in the CSV file
            Map(m => m.IsProcessing).Ignore();
        }
    }
}
