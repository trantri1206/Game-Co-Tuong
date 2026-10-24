using GameCoTuong.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameCoTuong.Data.Configurations;

public class MoveConfiguration : IEntityTypeConfiguration<Move>
{
    public void Configure(EntityTypeBuilder<Move> builder)
    {
        builder.ToTable("Moves");

        builder.HasKey(m => m.MoveId);

        builder.Property(m => m.MoveId)
            .ValueGeneratedOnAdd();

        builder.Property(m => m.MatchId)
            .IsRequired();

        builder.Property(m => m.UserId)
            .IsRequired();

        builder.Property(m => m.MoveOrder)
            .IsRequired();

        builder.Property(m => m.MoveNotation)
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(m => m.PlayedAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("getdate()");

        // Chặn nước đi trùng thứ tự trong cùng một ván cờ (UQ_Moves_Order)
        builder.HasIndex(m => new { m.MatchId, m.MoveOrder })
            .IsUnique()
            .HasDatabaseName("UQ_Moves_Order");

        builder.HasOne(m => m.Match)
            .WithMany(match => match.Moves)
            .HasForeignKey(m => m.MatchId)
            .HasConstraintName("FK_Moves_Matches")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany(u => u.Moves)
            .HasForeignKey(m => m.UserId)
            .HasConstraintName("FK_Moves_Users")
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
