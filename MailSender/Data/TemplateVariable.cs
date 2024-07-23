using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MailSender.Data
{
    [Table("TemplateVariables")]
    public class TemplateVariable
    {
        [Key]
        public int Id { get; set; }

        [Column("template_id")]
        public int TemplateID { get; set; }
        [ForeignKey("TemplateID")]
        public Template Template { get; set; }

        [Column("variable_id")]
        public int VariableID { get; set; }
        [ForeignKey("VariableID")]
        public Variable Variable { get; set; }
    }
}
