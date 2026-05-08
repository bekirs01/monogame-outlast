using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Outlast2D;

public class TileMap
{
    private int[,] _tiles;

    public int WidthInTiles { get; }
    public int HeightInTiles { get; }
    public int TileSizePixels { get; }

    public int WidthPixels => WidthInTiles * TileSizePixels;
    public int HeightPixels => HeightInTiles * TileSizePixels;

    public int ExitRoomIndexX { get; private set; }
    public int ExitRoomIndexY { get; private set; }

    /// <summary>Шаг на комнату (лабиринт внутри + 2 по краю). Должен совпадать с генерацией карты.</summary>
    public int RoomStepTiles { get; }

    /// <summary>Комнат на сторону: лёгкая 2 (2×2), сложная 3 (3×3).</summary>
    public int RoomsPerSide { get; }

    public int TilesPerRoomSide => RoomStepTiles + 1;

    public TileMap(int[,] tileData, int tileSizePixels, int exitRoomIndexX, int exitRoomIndexY, int roomStepTiles, int roomsPerSide = 3)
    {
        WidthInTiles = tileData.GetLength(0);
        HeightInTiles = tileData.GetLength(1);
        TileSizePixels = tileSizePixels;
        _tiles = tileData;
        ExitRoomIndexX = exitRoomIndexX;
        ExitRoomIndexY = exitRoomIndexY;
        RoomStepTiles = roomStepTiles;
        RoomsPerSide = roomsPerSide;
    }

    public int[,] CloneTiles()
    {
        int w = WidthInTiles;
        int h = HeightInTiles;
        var copy = new int[w, h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
                copy[x, y] = _tiles[x, y];
        }

        return copy;
    }

    public void ReplaceTiles(int[,] newTiles)
    {
        if (newTiles.GetLength(0) != WidthInTiles || newTiles.GetLength(1) != HeightInTiles)
            throw new ArgumentException("Tile grid boyutu uyuşmuyor.");
        _tiles = newTiles;
        RefreshExitRoomIndices();
    }

    private void RefreshExitRoomIndices()
    {
        for (int y = 0; y < HeightInTiles; y++)
        {
            for (int x = 0; x < WidthInTiles; x++)
            {
                if (_tiles[x, y] != 4)
                    continue;
                GetRoomIndexForTile(x, y, out int rx, out int ry);
                ExitRoomIndexX = rx;
                ExitRoomIndexY = ry;
                return;
            }
        }
    }

    /// <summary>Modül karışımından sonra oyuncu / harita işareti hücresini yürünebilir yap (duvar üstünde kalmayı önler).</summary>
    internal void EnsureWalkableAfterShuffle(int playerGx, int playerGy, int revealGx, int revealGy)
    {
        MapData.EnsureFloor(_tiles, playerGx, playerGy);
        MapData.EnsureFloor(_tiles, revealGx, revealGy);
    }

    public void GetRoomIndexForTile(int gridX, int gridY, out int roomX, out int roomY)
    {
        int max = RoomsPerSide - 1;
        int s = RoomStepTiles;
        roomX = Math.Clamp(gridX / s, 0, max);
        roomY = Math.Clamp(gridY / s, 0, max);
    }

    public static void GetRoomIndexForTile(int gridX, int gridY, int roomStepTiles, int roomsPerSide, out int roomX, out int roomY)
    {
        int max = roomsPerSide - 1;
        roomX = Math.Clamp(gridX / roomStepTiles, 0, max);
        roomY = Math.Clamp(gridY / roomStepTiles, 0, max);
    }

    public int RoomThemeIndexForDraw(int roomX, int roomY) => roomX + roomY * RoomsPerSide;

    public static int RoomThemeIndex(int roomX, int roomY, int roomsPerSide = 3) => roomX + roomY * roomsPerSide;

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

    /// <summary>Yalnızca düvar karosu (1) kırılır; kapı / çıkış / sandık dokunulmaz.</summary>
    public bool TryBreakWall(int gx, int gy)
    {
        if (gx < 0 || gx >= WidthInTiles || gy < 0 || gy >= HeightInTiles)
            return false;
        if (_tiles[gx, gy] != 1)
            return false;
        _tiles[gx, gy] = 0;
        return true;
    }

    /// <param name="floorByRoom">9 шт. — пол Kenney (64×64).</param>
    /// <param name="wallByRoom">9 шт. — стена Kenney.</param>
    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D[] floorByRoom,
        Texture2D[] wallByRoom,
        Texture2D doorClosed,
        Texture2D dungeonAtlas,
        Texture2D obstacleCrate,
        bool[,] chestOpened,
        ChestRewardKind[,] chestRewards,
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
                int ti = RoomThemeIndexForDraw(rx, ry);
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

                    if (chestRewards[x, y] == ChestRewardKind.Ammo)
                    {
                        Color tint = opened ? new Color(175, 175, 195) : Color.White;
                        if (animating)
                            tint = Color.Lerp(Color.White, tint, chestAnimT01);
                        spriteBatch.Draw(obstacleCrate, inner, null, tint);
                    }
                    else
                    {
                        var src = DungeonAtlasSprites.ChestFrameForDraw(opened, animating, chestAnimT01);
                        Color tint = opened ? new Color(210, 210, 210) : Color.White;
                        spriteBatch.Draw(dungeonAtlas, inner, src, tint);
                    }
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
