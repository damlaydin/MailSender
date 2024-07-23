using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MailSender.Data
{
    [Table("TemplateGroups")]
    public class TemplateGroup
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Template")]
        public int TemplateId { get; set; }
        public Template Template { get; set; }

        [ForeignKey("Group")]
        public int GroupId { get; set; }
        public Group Group { get; set; }
        
    }
}
