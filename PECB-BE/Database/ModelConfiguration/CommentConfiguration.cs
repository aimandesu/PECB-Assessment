using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PECB_BE.Entities;

namespace PECB_BE.Database.ModelConfiguration;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder
            .HasOne(c => c.Ticket)          // Comment has one Ticket
            .WithMany(t => t.Comments)         // Ticket has many Comments
            .HasForeignKey(c => c.TicketId)      // The string property "Ticket" holds the key
            .HasPrincipalKey(t => t.Id);
    }
}