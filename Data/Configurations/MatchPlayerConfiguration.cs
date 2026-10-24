using GameCoTuong.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameCoTuong.Data.Configurations;

public class MatchPlayerConfiguration : IEntityTypeConfiguration<MatchPlayer>
{
    public void Configure(EntityTypeBuilder<MatchPlayer> builder)
    {
        builder.ToTable("MatchPlayers");

        // Composite primary key (MatchId, UserId)
        builder.HasKey(mp => new { mp.MatchId, mp.UserId });

        builder.Property(mp => mp.Side)
            .HasMaxLength(10)
            .IsUnicode(false)
            .IsRequired();

        builder.HasOne(mp => mp.Match)
            .WithMany(m => m.MatchPlayers)
            .HasForeignKey(mp => mp.MatchId)
            .HasConstraintName("FK_MatchPlayers_Matches")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mp => mp.User)
            .WithMany(u => u.MatchPlayers)
            .HasForeignKey(mp => mp.UserId)
            .HasConstraintName("FK_MatchPlayers_Users")
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
