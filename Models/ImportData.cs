using System.ComponentModel.DataAnnotations.Schema;

namespace FileImportAPI.Models
{
    public class ImportData
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public string FileName { get; set; }
        public string UserName { get; set; }
        public string Location { get; set; }
        public DateTime ImportedAt { get; set; }
        public string FileHash { get; set; }
        public string RecordHash { get; set; }
        public bool IsProcessing { get; set; }
    }
}
