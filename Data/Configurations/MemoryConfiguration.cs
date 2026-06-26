using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoryMCP.Data.Configurations;

public class MemoryConfiguration : IEntityTypeConfiguration<Memory>
{
    public void Configure(EntityTypeBuilder<Memory> builder)
    {
        builder.ToTable("Memories");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Raw).IsRequired().HasMaxLength(8000);
        builder.Property(m => m.Created).IsRequired();
        builder.Property(m => m.StatusNote).HasMaxLength(2000);
        builder.HasIndex(m => m.Created);
        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.MemoryFrom);

        builder.HasOne(m => m.SupersedesMemory)
            .WithMany()
            .HasForeignKey(m => m.SupersedesMemoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.SupersededByMemory)
            .WithMany()
            .HasForeignKey(m => m.SupersededByMemoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
