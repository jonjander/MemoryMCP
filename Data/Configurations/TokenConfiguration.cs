using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoryMCP.Data.Configurations;

public class TokenConfiguration : IEntityTypeConfiguration<Token>
{
    public void Configure(EntityTypeBuilder<Token> builder)
    {
        builder.ToTable("Tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Property).IsRequired().HasMaxLength(256);
        builder.Property(t => t.Ref).HasMaxLength(RefIdGenerator.CharLength);
        builder.HasIndex(t => t.Ref).IsUnique().HasFilter("[Ref] IS NOT NULL");
        builder.Property(t => t.SearchValue).IsRequired().HasMaxLength(512);
        builder.Property(t => t.StringValue).HasMaxLength(1024);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => new { t.Property, t.IntValue });
        builder.HasIndex(t => new { t.Property, t.StringValue });
        builder.HasIndex(t => new { t.Property, t.SearchValue });
        builder.HasIndex(t => new { t.Property, t.Type, t.SearchValue });
        builder.HasIndex(t => t.Status);

        builder.HasOne(t => t.SupersedesToken)
            .WithMany()
            .HasForeignKey(t => t.SupersedesTokenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.SupersededByToken)
            .WithMany()
            .HasForeignKey(t => t.SupersededByTokenId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
