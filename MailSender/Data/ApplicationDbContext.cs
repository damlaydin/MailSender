using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MailSender.Data
{
    public class ApplicationDbContext : DbContext
    {
        public IConfiguration _config { get; set; }

        public ApplicationDbContext(IConfiguration config)
        {
            _config = config;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_config.GetConnectionString("DatabaseConnection"));
        }

        public DbSet<User> Users { get; set; }
        public DbSet<SentEmail> SentEmails { get; set; }
        public DbSet<EmailAttachment> EmailAttachments { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<TemplateImage> TemplateImages { get; set; }
        public DbSet<Variable> variables { get; set; }
        public DbSet<TemplateVariable> templateVariables { get; set; }
        public DbSet<TemplateGroup> TemplateGroups { get; set; }
        public DbSet<TemplateUser> TemplateUsers { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<SentEmail>()
                .HasMany(e => e.Attachments)
                .WithOne(a => a.SentEmail)
                .HasForeignKey(a => a.SentEmailId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TemplateGroup>()
                  .HasOne(tg => tg.Template)
                  .WithMany(t => t.TemplateGroups)
                  .HasForeignKey(tg => tg.TemplateId);

            modelBuilder.Entity<TemplateGroup>()
                .HasOne(tg => tg.Group)
                .WithMany(g => g.TemplateGroups)
                .HasForeignKey(tg => tg.GroupId);

            modelBuilder.Entity<TemplateUser>()
                .HasOne(tu => tu.Template)
                .WithMany(t => t.TemplateUsers)
                .HasForeignKey(tu => tu.TemplateId);

            modelBuilder.Entity<TemplateUser>()
                .HasOne(tu => tu.User)
                .WithMany(u => u.TemplateUsers)
                .HasForeignKey(tu => tu.UserId);

       

        }
    }
}

