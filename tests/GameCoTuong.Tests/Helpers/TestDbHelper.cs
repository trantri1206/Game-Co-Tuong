using GameCoTuong.Data;
using Microsoft.EntityFrameworkCore;

namespace GameCoTuong.Tests.Helpers;

public static class TestDbHelper
{
    /// <summary>
    /// Tạo một DbContext hoàn toàn cô lập cho mỗi lần chạy test bằng cách sinh tên database ngẫu nhiên.
    /// Đảm bảo bộ test chạy lặp lại vô số lần mà không bao giờ bị xung đột dữ liệu.
    /// </summary>
    public static CoTuongDbContext CreateIsolatedDbContext()
    {
        var options = new DbContextOptionsBuilder<CoTuongDbContext>()
            .UseInMemoryDatabase(databaseName: $"CoTuong_TestDb_{Guid.NewGuid():N}")
            .Options;

        var context = new CoTuongDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
