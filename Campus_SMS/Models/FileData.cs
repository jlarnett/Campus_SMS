namespace Campus_SMS.Models
{
    public class FileData
    {
        public int ID { get; set; }
        public string FileName { get; set; } = string.Empty;

        public FileData(string fileName) => FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
    }
}

