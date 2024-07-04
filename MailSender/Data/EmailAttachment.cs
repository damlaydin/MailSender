using System.ComponentModel.DataAnnotations.Schema;

namespace MailSender.Data
{
    [Table("EmailAttachment")]
    public class EmailAttachment
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Data { get; set; }
        public int SentEmailId { get; set; }
        public SentEmail SentEmail { get; set; }
    }
}
