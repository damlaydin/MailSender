using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace MailSender.Data
{
    [Table("Users")]
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public virtual ICollection<GroupMember> GroupMemberships { get; set; }

        public ICollection<TemplateUser> TemplateUsers { get; set; }

    }
}
