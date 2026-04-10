using System.Collections.Generic;
using Outlast2D;

namespace Outlast2D.Tests;

/// <summary>Вспомогательные проверки сетки карты без MonoGame UI.</summary>
internal static class MapTestHelpers
{
    /// <summary>Клетки, по которым может ходить игрок (стена и ящик-препятствие блокируют).</summary>
    internal static bool IsWalkableTile(int id) => id is not (1 or 5);

    internal static int CountTilesWithId(TileMap map, int id)
    {
        int n = 0;
        for (int y = 0; y < map.HeightInTiles; y++)
        {
            for (int x = 0; x < map.WidthInTiles; x++)
            {
                if (map.GetTileId(x, y) == id)
                    n++;
            }
        }

        return n;
    }

    /// <summary>Есть ли путь по клеткам, где игрок не блокируется (как <see cref="TileMap.BlocksPlayer"/>).</summary>
    /// <param name="keysHeld">Çıkış kilesine (4) basılı tutulduğunda varsayılan 3 — hedefe ulaşılabilirlik testi için.</param>
    internal static bool IsReachableForPlayer(TileMap map, int startX, int startY, int goalX, int goalY, int keysHeld = 3)
    {
        if (map.BlocksPlayer(startX, startY, keysHeld) || map.BlocksPlayer(goalX, goalY, keysHeld))
            return false;

        var q = new Queue<(int x, int y)>();
        var vis = new bool[map.WidthInTiles, map.HeightInTiles];
        q.Enqueue((startX, startY));
        vis[startX, startY] = true;

        while (q.Count > 0)
        {
            var (x, y) = q.Dequeue();
            if (x == goalX && y == goalY)
                return true;

            void Try(int nx, int ny)
            {
                if (nx < 0 || nx >= map.WidthInTiles || ny < 0 || ny >= map.HeightInTiles || vis[nx, ny])
                    return;
                if (map.BlocksPlayer(nx, ny, keysHeld))
                    return;
                vis[nx, ny] = true;
                q.Enqueue((nx, ny));
            }

            Try(x + 1, y);
            Try(x - 1, y);
            Try(x, y + 1);
            Try(x, y - 1);
        }

        return false;
    }

    internal static (int x, int y)? FindFirstTileWithId(TileMap map, int id)
    {
        for (int y = 0; y < map.HeightInTiles; y++)
        {
            for (int x = 0; x < map.WidthInTiles; x++)
            {
                if (map.GetTileId(x, y) == id)
                    return (x, y);
            }
        }

        return null;
    }

    internal static TileMap CreateAllFloorMap(int w, int h, int tilePx = 16)
    {
        var g = new int[w, h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
                g[x, y] = 0;
        }

        return new TileMap(g, tilePx, 0, 0, MapData.RoomStepTiles);
    }
}
