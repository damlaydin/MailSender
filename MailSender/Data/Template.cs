namespace MailSender.Data
{
    public class Template
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public ICollection<TemplateImage> TemplateImages { get; set; }
    }
}
