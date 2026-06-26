using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoryMCP.Data.Configurations;

public class MemoryTokenConfiguration : IEntityTypeConfiguration<MemoryToken>
{
    public void Configure(EntityTypeBuilder<MemoryToken> builder)
    {
        builder.ToTable("MemoryTokens");
        builder.HasKey(mt => mt.Id);

        builder.HasOne(mt => mt.Memory)
            .WithMany(m => m.Tokens)
            .HasForeignKey(mt => mt.MemoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mt => mt.Token)
            .WithMany(t => t.Memories)
            .HasForeignKey(mt => mt.TokenId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mt => new { mt.MemoryId, mt.TokenId }).IsUnique();
    }
}
