namespace MailSender.Data
{
    public class Template
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public ICollection<TemplateVariable> TemplateVariables { get; set; }

        public ICollection<TemplateGroup> TemplateGroups { get; set; }
        public ICollection<TemplateUser> TemplateUsers { get; set; }

    }
}
