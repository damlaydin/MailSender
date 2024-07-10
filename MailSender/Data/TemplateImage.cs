namespace MailSender.Data
{
    public class TemplateImage
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string ImagePath { get; set; }
        public Template Template { get; set; }
    }
}
