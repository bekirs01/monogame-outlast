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

    /// <summary>true ise sandık anahtar değil; açılınca fener menzili büyür (görünüş aynı karo 3).</summary>
    public required bool[,] ChestGrantsLantern { get; init; }
}
