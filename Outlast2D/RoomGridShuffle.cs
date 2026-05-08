using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Outlast2D;

/// <summary>
/// 3×3 haritada aynı boyuttaki modül blokları yer değiştirir; çıkış ve oyuncu birlikte taşınır.
/// </summary>
public static class RoomGridShuffle
{
    public static bool TryShuffle(
        TileMap map,
        bool[,] chestOpened,
        ChestRewardKind[,] chestRewards,
        Player player,
        ref int revealMarkerGridX,
        ref int revealMarkerGridY,
        Random rng)
    {
        const int roomsPerSide = 3;
        if (map.RoomsPerSide != roomsPerSide)
            return false;

        int step = map.RoomStepTiles;
        int roomInner = step - 2;
        int mapW = map.WidthInTiles;
        int mapH = map.HeightInTiles;
        int[,] oldSnapshot = map.CloneTiles();

        var rects = new Rectangle[9];
        var chunks = new int[9][,];
        for (int s = 0; s < 9; s++)
        {
            int rx = s % roomsPerSide;
            int ry = s / roomsPerSide;
            rects[s] = GetChunkRect(rx, ry, step, mapW, mapH, roomsPerSide);
            chunks[s] = Extract(oldSnapshot, rects[s]);
        }

        var receivesFrom = new int[9];
        for (int i = 0; i < 9; i++)
            receivesFrom[i] = i;

        var groups = new Dictionary<(int w, int h), List<int>>();
        for (int s = 0; s < 9; s++)
        {
            var key = (rects[s].Width, rects[s].Height);
            if (!groups.TryGetValue(key, out List<int> list))
            {
                list = new List<int>();
                groups[key] = list;
            }

            list.Add(s);
        }

        foreach (var kv in groups)
        {
            List<int> slots = kv.Value;
            int n = slots.Count;
            var perm = new int[n];
            for (int i = 0; i < n; i++)
                perm[i] = i;
            FisherYates(perm, rng);

            for (int i = 0; i < n; i++)
                receivesFrom[slots[i]] = slots[perm[i]];
        }

        var newGrid = new int[mapW, mapH];
        for (int y = 0; y < mapH; y++)
        {
            for (int x = 0; x < mapW; x++)
                newGrid[x, y] = 1;
        }

        for (int destSlot = 0; destSlot < 9; destSlot++)
        {
            int srcSlot = receivesFrom[destSlot];
            Paste(newGrid, rects[destSlot], chunks[srcSlot]);
        }

        MapData.ApplyInterRoomConnections(newGrid, roomInner, roomsPerSide);
        map.ReplaceTiles(newGrid);

        RemapActorAndChests(
            receivesFrom,
            rects,
            step,
            roomsPerSide,
            mapW,
            mapH,
            chestOpened,
            chestRewards,
            player,
            ref revealMarkerGridX,
            ref revealMarkerGridY);

        map.EnsureWalkableAfterShuffle(player.GridX, player.GridY, revealMarkerGridX, revealMarkerGridY);

        return true;
    }

    private static Rectangle GetChunkRect(int rx, int ry, int step, int mapW, int mapH, int roomsPerSide)
    {
        int x = rx * step;
        int y = ry * step;
        int w = rx < roomsPerSide - 1 ? step : mapW - (roomsPerSide - 1) * step;
        int h = ry < roomsPerSide - 1 ? step : mapH - (roomsPerSide - 1) * step;
        return new Rectangle(x, y, w, h);
    }

    private static int[,] Extract(int[,] grid, Rectangle r)
    {
        var data = new int[r.Width, r.Height];
        for (int iy = 0; iy < r.Height; iy++)
        {
            for (int ix = 0; ix < r.Width; ix++)
                data[ix, iy] = grid[r.X + ix, r.Y + iy];
        }

        return data;
    }

    private static void Paste(int[,] grid, Rectangle r, int[,] data)
    {
        for (int iy = 0; iy < r.Height; iy++)
        {
            for (int ix = 0; ix < r.Width; ix++)
                grid[r.X + ix, r.Y + iy] = data[ix, iy];
        }
    }

    private static void FisherYates(int[] a, Random rng)
    {
        for (int i = a.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (a[i], a[j]) = (a[j], a[i]);
        }
    }

    private static int SlotFromGridXY(int gx, int gy, int step, int roomsPerSide)
    {
        TileMap.GetRoomIndexForTile(gx, gy, step, roomsPerSide, out int rx, out int ry);
        return rx + ry * roomsPerSide;
    }

    private static int FindDestSlotForOriginal(int[] receivesFrom, int origSlot)
    {
        for (int d = 0; d < receivesFrom.Length; d++)
        {
            if (receivesFrom[d] == origSlot)
                return d;
        }

        return origSlot;
    }

    private static (int nx, int ny) Transform(
        int gx,
        int gy,
        int[] receivesFrom,
        Rectangle[] rects,
        int step,
        int roomsPerSide)
    {
        int origSlot = SlotFromGridXY(gx, gy, step, roomsPerSide);
        Rectangle ro = rects[origSlot];
        int lx = gx - ro.X;
        int ly = gy - ro.Y;
        int destSlot = FindDestSlotForOriginal(receivesFrom, origSlot);
        Rectangle rd = rects[destSlot];
        return (rd.X + lx, rd.Y + ly);
    }

    private static void RemapActorAndChests(
        int[] receivesFrom,
        Rectangle[] rects,
        int step,
        int roomsPerSide,
        int mapW,
        int mapH,
        bool[,] chestOpened,
        ChestRewardKind[,] chestRewards,
        Player player,
        ref int revealMarkerGridX,
        ref int revealMarkerGridY)
    {
        var newChest = new bool[mapW, mapH];
        var newRewards = new ChestRewardKind[mapW, mapH];

        for (int y = 0; y < mapH; y++)
        {
            for (int x = 0; x < mapW; x++)
            {
                if (!chestOpened[x, y])
                    continue;
                var t = Transform(x, y, receivesFrom, rects, step, roomsPerSide);
                if (t.nx >= 0 && t.nx < mapW && t.ny >= 0 && t.ny < mapH)
                    newChest[t.nx, t.ny] = true;
            }
        }

        for (int y = 0; y < mapH; y++)
        {
            for (int x = 0; x < mapW; x++)
            {
                ChestRewardKind k = chestRewards[x, y];
                if (k == ChestRewardKind.None)
                    continue;
                var t = Transform(x, y, receivesFrom, rects, step, roomsPerSide);
                if (t.nx >= 0 && t.nx < mapW && t.ny >= 0 && t.ny < mapH)
                    newRewards[t.nx, t.ny] = k;
            }
        }

        for (int y = 0; y < mapH; y++)
        {
            for (int x = 0; x < mapW; x++)
            {
                chestOpened[x, y] = newChest[x, y];
                chestRewards[x, y] = newRewards[x, y];
            }
        }

        var pt = Transform(player.GridX, player.GridY, receivesFrom, rects, step, roomsPerSide);
        player.SetGridPosition(pt.nx, pt.ny);

        var rv = Transform(revealMarkerGridX, revealMarkerGridY, receivesFrom, rects, step, roomsPerSide);
        revealMarkerGridX = rv.nx;
        revealMarkerGridY = rv.ny;
    }
}
