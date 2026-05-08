namespace Outlast2D;

/// <summary>Сгенерированная карта и индекс комнаты выхода для мини-карты.</summary>
public sealed class DungeonMapResult
{
    public required TileMap TileMap { get; init; }
    public int ExitRoomIndexX { get; init; }
    public int ExitRoomIndexY { get; init; }
    public int StartGridX { get; init; }
    public int StartGridY { get; init; }
    public int RevealMarkerGridX { get; init; }
    public int RevealMarkerGridY { get; init; }

    /// <summary>Her karo için sandık ödülü (yalnızca karo 3 olan hücrelerde dolu).</summary>
    public required ChestRewardKind[,] ChestRewards { get; init; }
}
