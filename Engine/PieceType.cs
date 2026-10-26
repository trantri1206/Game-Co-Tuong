namespace GameCoTuong.Engine;

/// <summary>
/// Bảy loại quân cờ cơ bản trong luật Cờ Tướng.
/// </summary>
public enum PieceType
{
    /// <summary>
    /// Tướng / Soái (帅 / 将)
    /// </summary>
    General = 1,

    /// <summary>
    /// Sĩ (仕 / 士)
    /// </summary>
    Advisor = 2,

    /// <summary>
    /// Tượng (相 / 象)
    /// </summary>
    Elephant = 3,

    /// <summary>
    /// Mã (傌 / 馬)
    /// </summary>
    Horse = 4,

    /// <summary>
    /// Xe (俥 / 車)
    /// </summary>
    Chariot = 5,

    /// <summary>
    /// Pháo (炮 / 砲)
    /// </summary>
    Cannon = 6,

    /// <summary>
    /// Tốt / Binh (兵 / 卒)
    /// </summary>
    Soldier = 7
}
