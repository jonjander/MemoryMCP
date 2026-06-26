using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoryMCP.Data.Configurations;

public class MemoryRevisionConfiguration : IEntityTypeConfiguration<MemoryRevision>
{
    public void Configure(EntityTypeBuilder<MemoryRevision> builder)
    {
        builder.ToTable("MemoryRevisions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Note).HasMaxLength(2000);
        builder.Property(r => r.SuccessorRaw).HasMaxLength(8000);
        builder.Property(r => r.Created).IsRequired();
        builder.HasIndex(r => r.MemoryId);
        builder.HasIndex(r => r.Created);

        builder.HasOne(r => r.Memory)
            .WithMany(m => m.Revisions)
            .HasForeignKey(r => r.MemoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
