using GameCoTuong.Data;
using GameCoTuong.Data.Seed;
using GameCoTuong.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== HỆ THỐNG QUẢN LÝ DỮ LIỆU CỜ TƯỚNG (EF CORE) ===");

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? "Server=.\\SQLEXPRESS;Database=CoTuongDB;Trusted_Connection=True;TrustServerCertificate=True;";

var optionsBuilder = new DbContextOptionsBuilder<CoTuongDbContext>();
optionsBuilder.UseSqlServer(connectionString);

using var context = new CoTuongDbContext(optionsBuilder.Options);

try
{
    var arg = args.FirstOrDefault()?.ToLowerInvariant();

    if (arg == "--test-constraints")
    {
        Console.WriteLine("\n[1/2] Đang kiểm tra ràng buộc: Chặn tài khoản trùng Username...");
        try
        {
            var dupUser = new User
            {
                Username = "player_red_demo", // Trùng username đã seed
                PasswordHash = "dummyhash",
                DisplayName = "Trùng lặp"
            };
            context.Users.Add(dupUser);
            await context.SaveChangesAsync();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("-> THẤT BẠI: Đã không chặn được tài khoản trùng!");
            Console.ResetColor();
        }
        catch (DbUpdateException ex)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("-> THÀNH CÔNG: Đã chặn tài khoản trùng Username đúng theo thiết kế!");
            Console.WriteLine($"   Chi tiết lỗi chặn: {ex.InnerException?.Message ?? ex.Message}");
            Console.ResetColor();
            // Detach duplicate entity to clear context
            context.ChangeTracker.Clear();
        }

        Console.WriteLine("\n[2/2] Đang kiểm tra ràng buộc: Chặn nước đi trùng thứ tự (MoveOrder) trong cùng ván cờ...");
        try
        {
            var existingMatch = await context.Matches.FirstOrDefaultAsync();
            var existingUser = await context.Users.FirstOrDefaultAsync();

            if (existingMatch != null && existingUser != null)
            {
                var dupMove = new Move
                {
                    MatchId = existingMatch.MatchId,
                    UserId = existingUser.UserId,
                    MoveOrder = 1, // Đã tồn tại MoveOrder = 1 trong ván đấu này
                    MoveNotation = "C2.5_DUP"
                };
                context.Moves.Add(dupMove);
                await context.SaveChangesAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Đã không chặn được nước đi trùng!");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("-> Cần chạy --seed trước khi test ràng buộc nước đi.");
            }
        }
        catch (DbUpdateException ex)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("-> THÀNH CÔNG: Đã chặn nước đi trùng thứ tự trong cùng ván cờ (UQ_Moves_Order)!");
            Console.WriteLine($"   Chi tiết lỗi chặn: {ex.InnerException?.Message ?? ex.Message}");
            Console.ResetColor();
            context.ChangeTracker.Clear();
        }

        return;
    }

    if (arg == "--seed")
    {
        Console.WriteLine("Đang nạp dữ liệu mẫu (Seed Data)...");
        await DbInitializer.SeedAsync(context);
        Console.WriteLine("Nạp dữ liệu mẫu hoàn tất!");
        return;
    }

    if (arg == "--migrate")
    {
        Console.WriteLine("Đang chạy migration lên SQL Server...");
        await context.Database.MigrateAsync();
        Console.WriteLine("Migration hoàn tất!");
        return;
    }

    Console.WriteLine("\nCác lệnh khả dụng:");
    Console.WriteLine("  dotnet ef migrations add <Name>   : Tạo migration mới");
    Console.WriteLine("  dotnet ef database update         : Cập nhật migration vào database");
    Console.WriteLine("  dotnet run -- --migrate           : Tự động chạy migration qua code");
    Console.WriteLine("  dotnet run -- --seed              : Nạp dữ liệu mẫu demo");
    Console.WriteLine("  dotnet run -- --test-constraints  : Kiểm tra kiểm thử ràng buộc trùng lặp");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n[Lỗi]: {ex.Message}");
    Console.ResetColor();
}
