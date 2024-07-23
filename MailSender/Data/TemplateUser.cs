using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MailSender.Data
{
    [Table("TemplateUsers")]
    public class TemplateUser
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Template")]
        public int TemplateId { get; set; }
        public Template Template { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
