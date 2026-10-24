using GameCoTuong.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameCoTuong.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.UserId);

        builder.Property(u => u.UserId)
            .ValueGeneratedOnAdd();

        builder.Property(u => u.Username)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(255)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .HasMaxLength(100)
            .IsUnicode(true);

        builder.Property(u => u.CreatedAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("getdate()");

        // Chặn tài khoản trùng thông qua Unique Index UQ_Users_Username
        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("UQ_Users_Username");

        // Chặn email trùng thông qua Unique Index UQ_Users_Email
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("UQ_Users_Email");
    }
}
