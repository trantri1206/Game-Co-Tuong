using GameCoTuong.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameCoTuong.Data.Configurations;

public class RoomMemberConfiguration : IEntityTypeConfiguration<RoomMember>
{
    public void Configure(EntityTypeBuilder<RoomMember> builder)
    {
        builder.ToTable("RoomMembers");

        // Composite primary key (RoomId, UserId)
        builder.HasKey(rm => new { rm.RoomId, rm.UserId });

        builder.Property(rm => rm.JoinedAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("getdate()");

        builder.HasOne(rm => rm.Room)
            .WithMany(r => r.RoomMembers)
            .HasForeignKey(rm => rm.RoomId)
            .HasConstraintName("FK_RoomMembers_Rooms")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rm => rm.User)
            .WithMany(u => u.RoomMembers)
            .HasForeignKey(rm => rm.UserId)
            .HasConstraintName("FK_RoomMembers_Users")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
