using Microsoft.EntityFrameworkCore.Migrations;

namespace MailSender.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Groups]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [Groups] (
                        [Id] int NOT NULL IDENTITY,
                        [GroupName] nvarchar(max) NOT NULL,
                        CONSTRAINT [PK_Groups] PRIMARY KEY ([Id])
                    )
                END
            ");


            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [Users] (
                        [Id] int NOT NULL IDENTITY,
                        [Email] nvarchar(250) NOT NULL,
                        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
                    )
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SentEmail]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [SentEmail] (
                        [Id] int NOT NULL IDENTITY,
                        [GroupEmails] nvarchar(max) NOT NULL,
                        [SenderEmail] nvarchar(max) NOT NULL,
                        [Subject] nvarchar(max) NOT NULL,
                        [Body] nvarchar(max) NOT NULL,
                        [SentDate] datetime2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT [PK_SentEmail] PRIMARY KEY ([Id])
                    )
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SentEmail");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Groups");
        }
    }
}
