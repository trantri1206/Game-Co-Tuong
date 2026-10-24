using GameCoTuong.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameCoTuong.Data.Configurations;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("Matches");

        builder.HasKey(m => m.MatchId);

        builder.Property(m => m.MatchId)
            .ValueGeneratedOnAdd();

        builder.Property(m => m.RoomId)
            .IsRequired(false);

        builder.Property(m => m.MatchType)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(m => m.InitialBoardState)
            .HasColumnType("varchar(max)")
            .IsUnicode(false)
            .IsRequired();

        builder.Property(m => m.Result)
            .HasMaxLength(50)
            .IsUnicode(false);

        builder.Property(m => m.Reason)
            .HasMaxLength(255)
            .IsUnicode(true);

        builder.Property(m => m.Version)
            .HasMaxLength(20)
            .IsUnicode(false)
            .HasDefaultValue("1.0");

        builder.Property(m => m.StartTime)
            .HasColumnType("datetime")
            .HasDefaultValueSql("getdate()");

        builder.Property(m => m.EndTime)
            .HasColumnType("datetime");

        builder.HasOne(m => m.Room)
            .WithMany(r => r.Matches)
            .HasForeignKey(m => m.RoomId)
            .HasConstraintName("FK_Matches_Rooms")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
