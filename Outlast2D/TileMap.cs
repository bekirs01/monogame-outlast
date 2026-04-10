using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Outlast2D;

public class TileMap
{
    private readonly int[,] _tiles;

    public int WidthInTiles { get; }
    public int HeightInTiles { get; }
    public int TileSizePixels { get; }

    public int WidthPixels => WidthInTiles * TileSizePixels;
    public int HeightPixels => HeightInTiles * TileSizePixels;

    public int ExitRoomIndexX { get; }
    public int ExitRoomIndexY { get; }

    public TileMap(int[,] tileData, int tileSizePixels, int exitRoomIndexX, int exitRoomIndexY)
    {
        WidthInTiles = tileData.GetLength(0);
        HeightInTiles = tileData.GetLength(1);
        TileSizePixels = tileSizePixels;
        _tiles = tileData;
        ExitRoomIndexX = exitRoomIndexX;
        ExitRoomIndexY = exitRoomIndexY;
    }

    public static void GetRoomIndexForTile(int gridX, int gridY, out int roomX, out int roomY)
    {
        int s = MapData.RoomStepTiles;
        roomX = Math.Clamp(gridX / s, 0, 2);
        roomY = Math.Clamp(gridY / s, 0, 2);
    }

    public static int RoomThemeIndex(int roomX, int roomY) => roomX + roomY * 3;

    public int GetTileId(int tileX, int tileY) => _tiles[tileX, tileY];

    public bool BlocksPlayer(int tileX, int tileY, int keysHeld = 0)
    {
        int id = _tiles[tileX, tileY];
        if (id == 1 || id == 5)
            return true;
        if (id == 4 && keysHeld < DungeonAtlasSprites.KeysToWin)
            return true;
        return false;
    }

    /// <param name="floorByRoom">9 adet — Kenney zemin (64×64).</param>
    /// <param name="wallByRoom">9 adet — Kenney duvar.</param>
    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D[] floorByRoom,
        Texture2D[] wallByRoom,
        Texture2D doorClosed,
        Texture2D dungeonAtlas,
        Texture2D obstacleCrate,
        bool[,] chestOpened,
        int animChestX,
        int animChestY,
        float chestAnimT01,
        int keysHeld)
    {
        int s = TileSizePixels;

        for (int y = 0; y < HeightInTiles; y++)
        {
            for (int x = 0; x < WidthInTiles; x++)
            {
                var dest = new Rectangle(x * s, y * s, s, s);
                GetRoomIndexForTile(x, y, out int rx, out int ry);
                int ti = RoomThemeIndex(rx, ry);
                Texture2D floorTex = floorByRoom[ti];
                Texture2D wallTex = wallByRoom[ti];

                int id = _tiles[x, y];

                if (id == 1)
                {
                    spriteBatch.Draw(wallTex, dest, null, Color.White);
                }
                else if (id == 0)
                {
                    spriteBatch.Draw(floorTex, dest, null, Color.White);
                }
                else if (id == 2)
                {
                    spriteBatch.Draw(floorTex, dest, null, Color.White);
                    spriteBatch.Draw(doorClosed, dest, null, Color.White);
                }
                else if (id == 3)
                {
                    spriteBatch.Draw(floorTex, dest, null, Color.White);
                    int inset = s / 10;
                    var inner = new Rectangle(x * s + inset, y * s + inset, s - 2 * inset, s - 2 * inset);
                    bool opened = chestOpened[x, y];
                    bool animating = !opened && animChestX == x && animChestY == y && chestAnimT01 > 0f;
                    var src = DungeonAtlasSprites.ChestFrameForDraw(opened, animating, chestAnimT01);
                    Color tint = opened ? new Color(210, 210, 210) : Color.White;
                    spriteBatch.Draw(dungeonAtlas, inner, src, tint);
                }
                else if (id == 4)
                {
                    spriteBatch.Draw(floorTex, dest, null, Color.White);
                    var doorSrc = keysHeld >= DungeonAtlasSprites.KeysToWin
                        ? DungeonAtlasSprites.DoorLeafOpen
                        : DungeonAtlasSprites.DoorLeafClosed;
                    spriteBatch.Draw(dungeonAtlas, dest, doorSrc, Color.White);
                }
                else if (id == 5)
                {
                    spriteBatch.Draw(floorTex, dest, null, Color.White);
                    int inset = s / 12;
                    var inner = new Rectangle(x * s + inset, y * s + inset, s - 2 * inset, s - 2 * inset);
                    spriteBatch.Draw(obstacleCrate, inner, null, Color.White);
                }
            }
        }
    }
}
