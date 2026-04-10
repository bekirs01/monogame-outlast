using System;
using System.Collections.Generic;

namespace Outlast2D;

/// <summary>Zor: 3×3 oda. Basit: 2×2 oda (aynı iç oda boyutu). Kenney görselleri TileMap çiziminde kullanılır.</summary>
public static class MapData
{
    public const int RoomInnerTiles = 17;
    public const int RoomStepTiles = RoomInnerTiles + 2;
    public const int TilesPerRoomSide = RoomStepTiles + 1;

    /// <summary>Basit harita: iç oda kare sayısı ≈ zor haritanın yarısı (17→8).</summary>
    public const int RoomInnerTilesEasy = 8;

    public const int MazeSeed = 424242;

    // 0 zemin, 1 duvar, 2 kapı, 3 sandık, 4 çıkış, 5 engel

    public static DungeonMapResult CreateDungeonMap(int tileSizePixels, MapDifficulty difficulty = MapDifficulty.Hard)
    {
        int roomInner = difficulty == MapDifficulty.Easy ? RoomInnerTilesEasy : RoomInnerTiles;
        int step = roomInner + 2;
        // Basit: 2×2 oda (4 oda, tam bağlantı); zor: 3×3.
        int roomsPerSide = difficulty == MapDifficulty.Easy ? 2 : 3;

        int startX = difficulty == MapDifficulty.Easy ? 4 : 8;
        int startY = difficulty == MapDifficulty.Easy ? 4 : 8;
        GetMiddleRoomRevealCell(step, roomInner, roomsPerSide, out int revealX, out int revealY);

        int mapW = step * roomsPerSide + 1;
        int mapH = step * roomsPerSide + 1;
        int mid = (roomInner + 1) / 2;

        var grid = new int[mapW, mapH];
        for (int y = 0; y < mapH; y++)
        {
            for (int x = 0; x < mapW; x++)
                grid[x, y] = 1;
        }

        for (int ry = 0; ry < roomsPerSide; ry++)
        {
            for (int rx = 0; rx < roomsPerSide; rx++)
            {
                int ox = rx * step;
                int oy = ry * step;
                CarveRoomMaze(grid, ox, oy, roomInner, new Random(GetRoomMazeSeed(rx, ry)));
            }
        }

        for (int ry = 0; ry < roomsPerSide; ry++)
        {
            for (int rx = 0; rx < roomsPerSide - 1; rx++)
            {
                int ox = rx * step;
                int oy = ry * step;
                int doorX = ox + roomInner + 1;
                int yDoor = oy + mid;
                grid[doorX, yDoor] = 2;
                grid[doorX + 1, yDoor] = 0;
            }
        }

        for (int ry = 0; ry < roomsPerSide - 1; ry++)
        {
            for (int rx = 0; rx < roomsPerSide; rx++)
            {
                int ox = rx * step;
                int oy = ry * step;
                int doorY = oy + roomInner + 1;
                int xDoor = ox + mid;
                grid[xDoor, doorY] = 2;
                grid[xDoor, doorY + 1] = 0;
            }
        }

        for (int ry = 0; ry < roomsPerSide; ry++)
        {
            for (int rx = 0; rx < roomsPerSide; rx++)
            {
                int ox = rx * step;
                int oy = ry * step;
                int yDoor = oy + mid;
                int xDoor = ox + mid;
                if (rx < roomsPerSide - 1)
                    grid[ox + roomInner, yDoor] = 0;
                if (rx > 0)
                    grid[ox + 1, yDoor] = 0;
                if (ry < roomsPerSide - 1)
                    grid[xDoor, oy + roomInner] = 0;
                if (ry > 0)
                    grid[xDoor, oy + 1] = 0;
            }
        }

        EnsureFloor(grid, startX, startY);
        EnsureFloor(grid, revealX, revealY);

        int worldSeed = unchecked(Environment.TickCount ^ (int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF));
        var rng = new Random(worldSeed);

        PlaceRandomObstaclesPreservingConnectivity(grid, mapW, mapH, rng, startX, startY, revealX, revealY, difficulty);

        if (!TryPlaceExit(grid, mapW, mapH, rng, startX, startY, out int exitX, out int exitY))
        {
            if (!TryPlaceExit(grid, mapW, mapH, new Random(0), startX, startY, out exitX, out exitY))
                throw new InvalidOperationException("Çıkış yeri bulunamadı.");
        }

        grid[exitX, exitY] = 4;

        PlaceChestsThreeRooms(grid, mapW, mapH, rng, roomInner, step, difficulty, startX, startY, exitX, exitY, revealX, revealY);

        GetRoomIndex(exitX, exitY, step, roomsPerSide, out int exitRoomX, out int exitRoomY);

        var tileData = (int[,])grid.Clone();
        var tileMap = new TileMap(tileData, tileSizePixels, exitRoomX, exitRoomY, step, roomsPerSide);

        return new DungeonMapResult
        {
            TileMap = tileMap,
            ExitRoomIndexX = exitRoomX,
            ExitRoomIndexY = exitRoomY,
            StartGridX = startX,
            StartGridY = startY,
            RevealMarkerGridX = revealX,
            RevealMarkerGridY = revealY,
        };
    }

    /// <summary>Orta oda iç labirent merkezi — 2×2 ve 3×3 için (1,1); üzerine gelince tüm harita açılır.</summary>
    private static void GetMiddleRoomRevealCell(int step, int roomInner, int roomsPerSide, out int gx, out int gy)
    {
        int rx = roomsPerSide / 2;
        int ry = roomsPerSide / 2;
        int ox = rx * step;
        int oy = ry * step;
        gx = ox + 1 + roomInner / 2;
        gy = oy + 1 + roomInner / 2;
    }

    private static int GetRoomMazeSeed(int rx, int ry) =>
        unchecked(MazeSeed + rx * 104729 + ry * 224737 + rx * ry * 1009);

    private static void GetRoomIndex(int gridX, int gridY, int step, int roomsPerSide, out int rx, out int ry)
    {
        int max = roomsPerSide - 1;
        rx = gridX / step;
        ry = gridY / step;
        if (rx > max)
            rx = max;
        if (ry > max)
            ry = max;
    }

    private static void EnsureFloor(int[,] grid, int x, int y)
    {
        if (x < 0 || x >= grid.GetLength(0) || y < 0 || y >= grid.GetLength(1))
            return;
        int id = grid[x, y];
        if (id == 2 || id == 3 || id == 4 || id == 5)
            return;
        grid[x, y] = 0;
    }

    private static void CarveRoomMaze(int[,] grid, int ox, int oy, int w, Random rng)
    {
        var pass = new bool[w, w];
        CarveMazePassages(pass, w, rng);

        for (int ly = 0; ly < w; ly++)
        {
            for (int lx = 0; lx < w; lx++)
                grid[ox + 1 + lx, oy + 1 + ly] = pass[lx, ly] ? 0 : 1;
        }
    }

    private static void CarveMazePassages(bool[,] pass, int n, Random rng)
    {
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
                pass[x, y] = false;
        }

        var visited = new bool[n, n];
        var stack = new Stack<(int x, int y)>();
        int sx = 1, sy = 1;
        visited[sx, sy] = true;
        pass[sx, sy] = true;
        stack.Push((sx, sy));

        var dirs = new (int dx, int dy)[] { (0, -2), (0, 2), (-2, 0), (2, 0) };

        while (stack.Count > 0)
        {
            var (cx, cy) = stack.Peek();
            var options = new List<(int nx, int ny, int wx, int wy)>();

            foreach (var (dx, dy) in dirs)
            {
                int mx = cx + dx;
                int my = cy + dy;
                if (mx < 1 || my < 1 || mx >= n - 1 || my >= n - 1)
                    continue;
                if (visited[mx, my])
                    continue;
                int bx = cx + dx / 2;
                int by = cy + dy / 2;
                options.Add((mx, my, bx, by));
            }

            if (options.Count == 0)
            {
                stack.Pop();
                continue;
            }

            int pick = rng.Next(options.Count);
            var (nx, ny, wx, wy) = options[pick];
            visited[nx, ny] = true;
            pass[wx, wy] = true;
            pass[nx, ny] = true;
            stack.Push((nx, ny));
        }
    }

    private static bool IsWalkableForConnectivity(int id) => id == 0 || id == 2;

    private static bool IsFullyConnected(int[,] grid, int mapW, int mapH, int startX, int startY)
    {
        if (!IsWalkableForConnectivity(grid[startX, startY]))
            return false;

        var q = new Queue<(int x, int y)>();
        var vis = new bool[mapW, mapH];
        q.Enqueue((startX, startY));
        vis[startX, startY] = true;
        int seen = 0;

        while (q.Count > 0)
        {
            var (x, y) = q.Dequeue();
            seen++;

            TryEnqueue(x + 1, y);
            TryEnqueue(x - 1, y);
            TryEnqueue(x, y + 1);
            TryEnqueue(x, y - 1);

            void TryEnqueue(int nx, int ny)
            {
                if (nx < 0 || nx >= mapW || ny < 0 || ny >= mapH || vis[nx, ny])
                    return;
                if (!IsWalkableForConnectivity(grid[nx, ny]))
                    return;
                vis[nx, ny] = true;
                q.Enqueue((nx, ny));
            }
        }

        int total = 0;
        for (int y = 0; y < mapH; y++)
        {
            for (int x = 0; x < mapW; x++)
            {
                if (IsWalkableForConnectivity(grid[x, y]))
                    total++;
            }
        }

        return seen == total;
    }

    private static void PlaceRandomObstaclesPreservingConnectivity(
        int[,] grid,
        int mapW,
        int mapH,
        Random rng,
        int protectX1,
        int protectY1,
        int protectX2,
        int protectY2,
        MapDifficulty difficulty)
    {
        var candidates = new List<(int x, int y)>();
        for (int y = 0; y < mapH; y++)
        {
            for (int x = 0; x < mapW; x++)
            {
                if (grid[x, y] != 0)
                    continue;
                if (x == protectX1 && y == protectY1)
                    continue;
                if (x == protectX2 && y == protectY2)
                    continue;
                candidates.Add((x, y));
            }
        }

        Shuffle(candidates, rng);

        int floorCells = candidates.Count;
        int minOb = difficulty == MapDifficulty.Easy ? 4 : 12;
        int maxOb = difficulty == MapDifficulty.Easy ? 18 : 48;
        int target = Math.Min(maxOb, Math.Max(minOb, floorCells / 4));
        int placed = 0;

        foreach (var (x, y) in candidates)
        {
            if (placed >= target)
                break;
            grid[x, y] = 5;
            if (!IsFullyConnected(grid, mapW, mapH, protectX1, protectY1))
                grid[x, y] = 0;
            else
                placed++;
        }
    }

    private static void Shuffle(List<(int x, int y)> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>(8,8) ile aynı zemin+kapı bileşenindeki hücreler — çıkış burada olmalı.</summary>
    private static bool[,] ReachableFloorAndDoorsMask(int[,] grid, int mapW, int mapH, int sx, int sy)
    {
        var vis = new bool[mapW, mapH];
        if (!IsWalkableForConnectivity(grid[sx, sy]))
            return vis;

        var q = new Queue<(int x, int y)>();
        q.Enqueue((sx, sy));
        vis[sx, sy] = true;

        while (q.Count > 0)
        {
            var (x, y) = q.Dequeue();

            void Try(int nx, int ny)
            {
                if (nx < 0 || nx >= mapW || ny < 0 || ny >= mapH || vis[nx, ny])
                    return;
                if (!IsWalkableForConnectivity(grid[nx, ny]))
                    return;
                vis[nx, ny] = true;
                q.Enqueue((nx, ny));
            }

            Try(x + 1, y);
            Try(x - 1, y);
            Try(x, y + 1);
            Try(x, y - 1);
        }

        return vis;
    }

    private static bool TryPlaceExit(int[,] grid, int mapW, int mapH, Random rng, int startX, int startY, out int exitX, out int exitY)
    {
        var reach = ReachableFloorAndDoorsMask(grid, mapW, mapH, startX, startY);

        var candidates = new List<(int x, int y)>();
        for (int y = 0; y < mapH; y++)
        {
            for (int x = 0; x < mapW; x++)
            {
                if (grid[x, y] != 0)
                    continue;
                if (x == startX && y == startY)
                    continue;
                if (!reach[x, y])
                    continue;
                candidates.Add((x, y));
            }
        }

        if (candidates.Count == 0)
        {
            exitX = exitY = 0;
            return false;
        }

        var pick = candidates[rng.Next(candidates.Count)];
        exitX = pick.x;
        exitY = pick.y;
        return true;
    }

    private static int CountWalkableNeighbors(int[,] g, int x, int y)
    {
        int n = 0;
        void C(int nx, int ny)
        {
            if (nx < 0 || nx >= g.GetLength(0) || ny < 0 || ny >= g.GetLength(1))
                return;
            int id = g[nx, ny];
            if (id == 1 || id == 5)
                return;
            if (id == 0 || id == 2 || id == 4)
                n++;
        }

        C(x + 1, y);
        C(x - 1, y);
        C(x, y + 1);
        C(x, y - 1);
        return n;
    }

    /// <summary>Üç sandık: zor modda köşegen (0,0),(1,1),(2,2); basit 2×2’de üç farklı oda.</summary>
    private static void PlaceChestsThreeRooms(
        int[,] grid,
        int mapW,
        int mapH,
        Random rng,
        int roomInner,
        int step,
        MapDifficulty difficulty,
        int startX,
        int startY,
        int exitX,
        int exitY,
        int revealX,
        int revealY)
    {
        var exclude = new HashSet<(int x, int y)>
        {
            (startX, startY),
            (exitX, exitY),
            (revealX, revealY),
        };

        ReadOnlySpan<(int rx, int ry)> targetRooms = difficulty == MapDifficulty.Easy
            ? stackalloc[] { (0, 0), (1, 0), (0, 1) }
            : stackalloc[] { (0, 0), (1, 1), (2, 2) };
        int placedCount = 0;

        foreach (var (rx, ry) in targetRooms)
        {
            if (TryPlaceChestInRoom(grid, mapW, mapH, roomInner, step, rx, ry, exclude, out int cx, out int cy))
            {
                grid[cx, cy] = 3;
                placedCount++;
                exclude.Add((cx, cy));
            }
        }

        if (placedCount < 3)
            PlaceChestsInDeadEndsFallback(grid, mapW, mapH, rng, startX, startY, exitX, exitY, revealX, revealY, 3 - placedCount);
    }

    private static bool TryPlaceChestInRoom(
        int[,] grid,
        int mapW,
        int mapH,
        int roomInner,
        int step,
        int rx,
        int ry,
        HashSet<(int x, int y)> exclude,
        out int cx,
        out int cy)
    {
        int ox = rx * step;
        int oy = ry * step;
        var deadEnds = new List<(int x, int y)>();
        var floors = new List<(int x, int y)>();

        for (int ly = 1; ly <= roomInner; ly++)
        {
            for (int lx = 1; lx <= roomInner; lx++)
            {
                int gx = ox + lx;
                int gy = oy + ly;
                if (gx < 0 || gx >= mapW || gy < 0 || gy >= mapH)
                    continue;
                if (grid[gx, gy] != 0)
                    continue;
                if (exclude.Contains((gx, gy)))
                    continue;

                floors.Add((gx, gy));
                int deg = CountWalkableNeighbors(grid, gx, gy);
                if (deg == 1)
                    deadEnds.Add((gx, gy));
            }
        }

        deadEnds.Sort(static (a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        floors.Sort(static (a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

        if (deadEnds.Count > 0)
        {
            cx = deadEnds[0].x;
            cy = deadEnds[0].y;
            return true;
        }

        if (floors.Count > 0)
        {
            cx = floors[0].x;
            cy = floors[0].y;
            return true;
        }

        cx = cy = 0;
        return false;
    }

    private static void PlaceChestsInDeadEndsFallback(
        int[,] grid,
        int mapW,
        int mapH,
        Random rng,
        int startX,
        int startY,
        int exitX,
        int exitY,
        int revealX,
        int revealY,
        int needMore)
    {
        if (needMore <= 0)
            return;

        var deadEnds = new List<(int x, int y)>();
        var secondary = new List<(int x, int y)>();

        for (int y = 0; y < mapH; y++)
        {
            for (int x = 0; x < mapW; x++)
            {
                if (grid[x, y] != 0)
                    continue;
                if (x == startX && y == startY)
                    continue;
                if (x == exitX && y == exitY)
                    continue;
                if (x == revealX && y == revealY)
                    continue;

                int deg = CountWalkableNeighbors(grid, x, y);
                if (deg == 1)
                    deadEnds.Add((x, y));
                else if (deg == 2)
                    secondary.Add((x, y));
            }
        }

        Shuffle(deadEnds, rng);
        Shuffle(secondary, rng);

        var picked = new List<(int x, int y)>();
        foreach (var p in deadEnds)
        {
            if (picked.Count >= needMore + 6)
                break;
            picked.Add(p);
        }

        foreach (var p in secondary)
        {
            if (picked.Count >= needMore + 12)
                break;
            if (!picked.Contains(p))
                picked.Add(p);
        }

        if (picked.Count < needMore)
        {
            var any = new List<(int x, int y)>();
            for (int y = 0; y < mapH; y++)
            {
                for (int x = 0; x < mapW; x++)
                {
                    if (grid[x, y] != 0)
                        continue;
                    if (x == startX && y == startY)
                        continue;
                    if (x == exitX && y == exitY)
                        continue;
                    if (picked.Contains((x, y)))
                        continue;
                    any.Add((x, y));
                }
            }

            Shuffle(any, rng);
            foreach (var p in any)
            {
                if (picked.Count >= needMore + 24)
                    break;
                picked.Add(p);
            }
        }

        int placed = 0;
        foreach (var (px, py) in picked)
        {
            if (placed >= needMore)
                break;
            if (grid[px, py] != 0)
                continue;
            grid[px, py] = 3;
            placed++;
        }
    }
}
