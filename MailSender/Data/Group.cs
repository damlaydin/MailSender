
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace MailSender.Data
{
    [Table("Groups")]
    public class Group
    {
        public int Id { get; set; }

        public string GroupName { get; set; }

        public ICollection<GroupMember> GroupMembers { get; set; }
    }
}
