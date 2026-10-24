using GameCoTuong.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameCoTuong.Data.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");

        builder.HasKey(r => r.RoomId);

        builder.Property(r => r.RoomId)
            .ValueGeneratedOnAdd();

        builder.Property(r => r.RoomName)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(r => r.IsActive)
            .HasDefaultValue(true);

        builder.Property(r => r.CreatedAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("getdate()");
    }
}
