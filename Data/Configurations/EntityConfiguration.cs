using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoryMCP.Data.Configurations;

public class EntityConfiguration : IEntityTypeConfiguration<Entity>
{
    public void Configure(EntityTypeBuilder<Entity> builder)
    {
        builder.ToTable("Entities");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(512);
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.Type, e.Name }).IsUnique()
            .HasFilter("[Status] = 0");

        builder.HasOne(e => e.MergedIntoEntity)
            .WithMany()
            .HasForeignKey(e => e.MergedIntoEntityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
