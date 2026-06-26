using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoryMCP.Data.Configurations;

public class EntityRelationshipConfiguration : IEntityTypeConfiguration<EntityRelationship>
{
    public void Configure(EntityTypeBuilder<EntityRelationship> builder)
    {
        builder.ToTable("EntityRelationships");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RelationType).IsRequired().HasMaxLength(128);
        builder.Property(r => r.Created).IsRequired();

        builder.HasOne(r => r.FromEntity)
            .WithMany(e => e.OutgoingRelationships)
            .HasForeignKey(r => r.FromEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ToEntity)
            .WithMany(e => e.IncomingRelationships)
            .HasForeignKey(r => r.ToEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Memory)
            .WithMany()
            .HasForeignKey(r => r.MemoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.FromEntityId, r.RelationType });
        builder.HasIndex(r => new { r.ToEntityId, r.RelationType });
        builder.HasIndex(r => new { r.FromEntityId, r.ToEntityId, r.RelationType });
    }
}
