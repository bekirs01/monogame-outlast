namespace Outlast2D;

/// <summary>Kenney Scribble Dungeons — her (rx,ry) odası için ayrı zemin + duvar PNG adları (64×64).</summary>
public static class KenneyRoomThemes
{
    /// <summary>9 oda: (zemin dosya adı uzantısız, duvar dosya adı uzantısız).</summary>
    public static readonly (string FloorBase, string WallBase)[] RoomPairs =
    {
        ("tile", "wall_edge"),
        ("floor_planks", "wall_demolished"),
        ("carpet", "wall_diagonal"),
        ("grass", "wall_curve"),
        ("floor_puddle", "wall_damaged"),
        ("wood", "wall_trap"),
        ("floor_path", "floor_wall"),
        ("floor_coffin", "tiles_corner"),
        ("floor_plants", "inner_round"),
    };

    public static string FileName(string baseName) => baseName + ".png";
}
