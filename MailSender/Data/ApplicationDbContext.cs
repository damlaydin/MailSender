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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SentEmail>()
                .HasMany(e => e.Attachments)
                .WithOne(a => a.SentEmail)
                .HasForeignKey(a => a.SentEmailId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

