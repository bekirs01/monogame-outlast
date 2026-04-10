namespace Outlast2D;

/// <summary>Kenney Scribble Dungeons — для каждой (rx,ry) отдельные пол и стена PNG (64×64).</summary>
public static class KenneyRoomThemes
{
    /// <summary>9 комнат: (базовое имя пола без расширения, базовое имя стены без расширения).</summary>
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
