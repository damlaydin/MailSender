using System.ComponentModel.DataAnnotations.Schema;

namespace MailSender.Data
{
    [Table("Variables")]
    public class Variable
    {
        public int Id { get; set; }

        [Column("var_name")]
        public string Var_name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        public ICollection<TemplateVariable> TemplateVariables { get; set; }

    }
}
