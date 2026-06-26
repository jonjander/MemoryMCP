using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoryMCP.Data.Configurations;

public class TokenRevisionConfiguration : IEntityTypeConfiguration<TokenRevision>
{
    public void Configure(EntityTypeBuilder<TokenRevision> builder)
    {
        builder.ToTable("TokenRevisions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Note).HasMaxLength(2000);
        builder.Property(r => r.PreviousProperty).HasMaxLength(256);
        builder.Property(r => r.NewProperty).HasMaxLength(256);
        builder.Property(r => r.PreviousSearchValue).HasMaxLength(512);
        builder.Property(r => r.NewSearchValue).HasMaxLength(512);
        builder.Property(r => r.Created).IsRequired();
        builder.HasIndex(r => r.TokenId);
        builder.HasIndex(r => r.Created);

        builder.HasOne(r => r.Token)
            .WithMany(t => t.Revisions)
            .HasForeignKey(r => r.TokenId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
