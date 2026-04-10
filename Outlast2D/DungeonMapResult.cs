namespace Outlast2D;

/// <summary>Üretilen harita ve minimap için çıkış oda indeksi.</summary>
public sealed class DungeonMapResult
{
    public required TileMap TileMap { get; init; }
    public int ExitRoomIndexX { get; init; }
    public int ExitRoomIndexY { get; init; }
    public int StartGridX { get; init; }
    public int StartGridY { get; init; }
    public int RevealMarkerGridX { get; init; }
    public int RevealMarkerGridY { get; init; }
}
