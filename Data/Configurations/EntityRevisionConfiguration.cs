using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoryMCP.Data.Configurations;

public class EntityRevisionConfiguration : IEntityTypeConfiguration<EntityRevision>
{
    public void Configure(EntityTypeBuilder<EntityRevision> builder)
    {
        builder.ToTable("EntityRevisions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Note).HasMaxLength(2000);
        builder.Property(r => r.PreviousName).HasMaxLength(512);
        builder.Property(r => r.NewName).HasMaxLength(512);
        builder.Property(r => r.PreviousType).HasMaxLength(128);
        builder.Property(r => r.NewType).HasMaxLength(128);
        builder.Property(r => r.Created).IsRequired();
        builder.HasIndex(r => r.EntityId);
        builder.HasIndex(r => r.Created);

        builder.HasOne(r => r.Entity)
            .WithMany(e => e.Revisions)
            .HasForeignKey(r => r.EntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
