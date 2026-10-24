using GameCoTuong.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameCoTuong.Data;

public class CoTuongDbContext : DbContext
{
    public CoTuongDbContext(DbContextOptions<CoTuongDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomMember> RoomMembers => Set<RoomMember>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchPlayer> MatchPlayers => Set<MatchPlayer>();
    public DbSet<Move> Moves => Set<Move>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Áp dụng tất cả các cấu hình Fluent API từ Configurations assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoTuongDbContext).Assembly);
    }
}
