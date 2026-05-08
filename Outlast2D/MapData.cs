using System;
using System.Collections.Generic;

namespace Outlast2D;

/// <summary>Сложная: сетка 3×3. Лёгкая: 2×2 (тот же размер комнаты внутри). Текстуры Kenney рисует TileMap.</summary>
public static class MapData
{
    public const int RoomInnerTiles = 17;
    public const int RoomStepTiles = RoomInnerTiles + 2;
    public const int TilesPerRoomSide = RoomStepTiles + 1;

    /// <summary>Лёгкая карта: клеток внутри комнаты ≈ половина сложной (17→8).</summary>
    public const int RoomInnerTilesEasy = 8;

    public const int MazeSeed = 424242;

    // 0 пол, 1 стена, 2 дверь, 3 сундук, 4 выход, 5 препятствие

    public static DungeonMapResult CreateDungeonMap(int tileSizePixels, MapDifficulty difficulty = MapDifficulty.Hard)
    {
        int roomInner = difficulty == MapDifficulty.Easy ? RoomInnerTilesEasy : RoomInnerTiles;
        int step = roomInner + 2;
        // Лёгкая: сетка 2×2; сложная: 3×3.
        int roomsPerSide = difficulty == MapDifficulty.Easy ? 2 : 3;

        int startX = difficulty == MapDifficulty.Easy ? 4 : 8;
        int startY = difficulty == MapDifficulty.Easy ? 4 : 8;
        GetMiddleRoomRevealCell(step, roomInner, roomsPerSide, out int revealX, out int revealY);

        int mapW = step * roomsPerSide + 1;
        int mapH = step * roomsPerSide + 1;

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

        ApplyInterRoomConnections(grid, roomInner, roomsPerSide);

        // Zor harita: komşu oda kapıları ile iç labirent arasında mutlaka yürünebilir koridor olsun;
        // randımanlı engeller kapı–sandık hattını sık sık kesiyordu.
        if (difficulty == MapDifficulty.Hard)
            EnsureHardRoomPortalLinks(grid, roomInner, step, roomsPerSide);

        EnsureFloor(grid, startX, startY);
        EnsureFloor(grid, revealX, revealY);

        int worldSeed = unchecked(Environment.TickCount ^ (int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF));
        var rng = new Random(worldSeed);

        PlaceRandomObstaclesPreservingConnectivity(grid, mapW, mapH, rng, startX, startY, revealX, revealY, difficulty);

        if (!TryPlaceExit(grid, mapW, mapH, rng, startX, startY, out int exitX, out int exitY))
        {
            if (!TryPlaceExit(grid, mapW, mapH, new Random(0), startX, startY, out exitX, out exitY))
                throw new InvalidOperationException("Место выхода не найдено.");
        }

        grid[exitX, exitY] = 4;

        var chestRewards = new ChestRewardKind[mapW, mapH];
        PlaceChestsFiveWithKinds(
            grid,
            chestRewards,
            mapW,
            mapH,
            rng,
            roomInner,
            step,
            difficulty,
            startX,
            startY,
            exitX,
            exitY,
            revealX,
            revealY);

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
            ChestRewards = chestRewards,
        };
    }

    /// <summary>Центр лабиринта средней комнаты — (1,1) для 2×2 и 3×3; при наступлении открывается вся карта.</summary>
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

    /// <summary>Komşu odalar arası kapılar ve köşe geçişleri (yer değiştirmeden sonra yeniden uygulanır).</summary>
    public static void ApplyInterRoomConnections(int[,] grid, int roomInner, int roomsPerSide)
    {
        int step = roomInner + 2;
        int mid = (roomInner + 1) / 2;

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
    }

    internal static void EnsureFloor(int[,] grid, int x, int y)
    {
        if (x < 0 || x >= grid.GetLength(0) || y < 0 || y >= grid.GetLength(1))
            return;
        int id = grid[x, y];
        if (id == 2 || id == 3 || id == 4 || id == 5)
            return;
        grid[x, y] = 0;
    }

    /// <summary>Oda merkezinden her komşu haritaya açılan kapı hattına Manhattan koridor (yalnızca Hard).</summary>
    private static void EnsureHardRoomPortalLinks(int[,] grid, int roomInner, int step, int roomsPerSide)
    {
        int mid = (roomInner + 1) / 2;
        for (int ry = 0; ry < roomsPerSide; ry++)
        {
            for (int rx = 0; rx < roomsPerSide; rx++)
            {
                int ox = rx * step;
                int oy = ry * step;
                int hx = ox + mid;
                int hy = oy + mid;

                if (rx < roomsPerSide - 1)
                    HardCarveCorridor(grid, hx, hy, ox + roomInner, oy + mid);
                if (rx > 0)
                    HardCarveCorridor(grid, hx, hy, ox + 1, oy + mid);
                if (ry < roomsPerSide - 1)
                    HardCarveCorridor(grid, hx, hy, ox + mid, oy + roomInner);
                if (ry > 0)
                    HardCarveCorridor(grid, hx, hy, ox + mid, oy + 1);
            }
        }
    }

    /// <summary>Kapı karosu (2) korunur; duvar ve kasalar/çıkış öncesi engeller temizlenir.</summary>
    private static void HardClearWalkTile(int[,] grid, int x, int y)
    {
        if (grid[x, y] == 2)
            return;
        grid[x, y] = 0;
    }

    private static void HardCarveCorridor(int[,] grid, int x0, int y0, int x1, int y1)
    {
        int x = x0, y = y0;
        HardClearWalkTile(grid, x, y);
        while (x != x1 || y != y1)
        {
            if (x < x1)
                x++;
            else if (x > x1)
                x--;
            else if (y < y1)
                y++;
            else if (y > y1)
                y--;
            HardClearWalkTile(grid, x, y);
        }
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
        // Zor modda kasalar ve çıkış her zaman açık bağlantıyla kalsın (dar tüneller kapanmasın).
        if (difficulty == MapDifficulty.Hard)
            return;

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

    /// <summary>Клетки той же связной области пола и дверей, что и старт (напр. 8,8) — выход должен быть здесь.</summary>
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

    private const int TotalGameplayChests = 5;
    private const int KeyChestTarget = 2;
    private const int LanternChestTarget = 2;
    private const int AmmoChestTarget = 1;

    /// <summary>Beş sandık: iki anahtar, iki fener, bir mermi kasası (mermi sandığı görünüş olarak Kasayarak).</summary>
    private static void PlaceChestsFiveWithKinds(
        int[,] grid,
        ChestRewardKind[,] chestRewards,
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

        ReadOnlySpan<(int rx, int ry, ChestRewardKind kind)> plan = difficulty == MapDifficulty.Easy
            ? stackalloc[]
            {
                (0, 0, ChestRewardKind.Key),
                (1, 1, ChestRewardKind.Key),
                (1, 0, ChestRewardKind.Lantern),
                (0, 1, ChestRewardKind.Lantern),
                (1, 1, ChestRewardKind.Ammo),
            }
            : stackalloc[]
            {
                (0, 0, ChestRewardKind.Key),
                (1, 1, ChestRewardKind.Key),
                (2, 2, ChestRewardKind.Lantern),
                (2, 0, ChestRewardKind.Lantern),
                (0, 2, ChestRewardKind.Ammo),
            };

        int keysPlaced = 0;
        int lanternsPlaced = 0;
        int ammoPlaced = 0;

        for (int i = 0; i < plan.Length; i++)
        {
            var (rx, ry, kind) = plan[i];
            if (!TryPlaceChestInRoom(grid, mapW, mapH, roomInner, step, rx, ry, exclude, out int cx, out int cy))
                continue;
            grid[cx, cy] = 3;
            chestRewards[cx, cy] = kind;
            exclude.Add((cx, cy));
            switch (kind)
            {
                case ChestRewardKind.Key:
                    keysPlaced++;
                    break;
                case ChestRewardKind.Lantern:
                    lanternsPlaced++;
                    break;
                case ChestRewardKind.Ammo:
                    ammoPlaced++;
                    break;
            }
        }

        int needKeys = KeyChestTarget - keysPlaced;
        int needLanterns = LanternChestTarget - lanternsPlaced;
        int needAmmo = AmmoChestTarget - ammoPlaced;
        int placedTotal = keysPlaced + lanternsPlaced + ammoPlaced;
        int needTotal = needKeys + needLanterns + needAmmo;
        if (needTotal > 0 && placedTotal < TotalGameplayChests)
            PlaceChestsInDeadEndsFallback(
                grid,
                chestRewards,
                mapW,
                mapH,
                rng,
                startX,
                startY,
                exitX,
                exitY,
                revealX,
                revealY,
                needKeys,
                needLanterns,
                needAmmo);
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
        ChestRewardKind[,] chestRewards,
        int mapW,
        int mapH,
        Random rng,
        int startX,
        int startY,
        int exitX,
        int exitY,
        int revealX,
        int revealY,
        int needMoreKeys,
        int needMoreLanterns,
        int needMoreAmmo)
    {
        int needMore = needMoreKeys + needMoreLanterns + needMoreAmmo;
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
            ChestRewardKind rk = ChestRewardKind.Key;
            if (placed < needMoreKeys)
                rk = ChestRewardKind.Key;
            else if (placed < needMoreKeys + needMoreLanterns)
                rk = ChestRewardKind.Lantern;
            else
                rk = ChestRewardKind.Ammo;
            chestRewards[px, py] = rk;
            placed++;
        }
    }
}
