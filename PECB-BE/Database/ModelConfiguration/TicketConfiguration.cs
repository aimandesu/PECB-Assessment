using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PECB_BE.Entities;

namespace PECB_BE.Database.ModelConfiguration;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder
            .Property<int>("TicketNumber")
            .UseIdentityColumn();

        builder
            .Property("CreatedDate")
            .HasColumnName("CreatedDate")
            .IsRequired();

        builder
            .Property(t => t.Reference)
            .HasComputedColumnSql(
                "CONCAT('TCK-', YEAR([CreatedDate]), '-', RIGHT('0000' + CAST([TicketNumber] AS VARCHAR(20)), 4))",
                stored: true);

        builder
            .HasIndex(t => t.Reference)
            .IsUnique();
    }
}