using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoryMCP.Data.Configurations;

public class MemoryEntityConfiguration : IEntityTypeConfiguration<MemoryEntity>
{
    public void Configure(EntityTypeBuilder<MemoryEntity> builder)
    {
        builder.ToTable("MemoryEntities");
        builder.HasKey(me => new { me.MemoryId, me.EntityId });

        builder.HasOne(me => me.Memory)
            .WithMany(m => m.Entities)
            .HasForeignKey(me => me.MemoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(me => me.Entity)
            .WithMany(e => e.Memories)
            .HasForeignKey(me => me.EntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
