using System;
using Microsoft.Xna.Framework;

namespace Outlast2D;

/// <summary>0x72 DungeonTilesetII_v1.7 — координаты tile_list_v1.7.</summary>
public static class DungeonAtlasSprites
{
    public const int KeysToWin = 2;

    /// <summary>Mermi sandığından gelen şarjör boyutu.</summary>
    public const int AmmoPerAmmoChest = 5;

    public static readonly Rectangle ChestFullClosed = new Rectangle(304, 416, 16, 16);
    public static readonly Rectangle[] ChestFullOpenFrames =
    {
        new Rectangle(304, 416, 16, 16),
        new Rectangle(320, 416, 16, 16),
        new Rectangle(336, 416, 16, 16),
    };

    public static readonly Rectangle DoorLeafClosed = new Rectangle(32, 240, 32, 32);
    public static readonly Rectangle DoorLeafOpen = new Rectangle(80, 240, 32, 32);

    /// <summary>Золотой ключ / монета награды.</summary>
    public static readonly Rectangle CoinKey = new Rectangle(289, 385, 6, 7);

    public static Rectangle ChestFrameForDraw(bool opened, bool isAnimating, float animT01)
    {
        if (opened)
            return ChestFullOpenFrames[2];
        if (isAnimating)
        {
            int i = Math.Min((int)(animT01 * 3f), 2);
            return ChestFullOpenFrames[i];
        }

        return ChestFullClosed;
    }
}
