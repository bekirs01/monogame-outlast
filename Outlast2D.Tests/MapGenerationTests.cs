using Outlast2D;
using Xunit;

namespace Outlast2D.Tests;

/// <summary>Интеграционные проверки <see cref="MapData.CreateDungeonMap"/>.</summary>
public class MapGenerationTests
{
    [Fact]
    public void CreateDungeonMap_produces_expected_size_and_start_walkable()
    {
        var result = MapData.CreateDungeonMap(24);
        var map = result.TileMap;

        int expected = MapData.RoomStepTiles * 3 + 1;
        Assert.Equal(expected, map.WidthInTiles);
        Assert.Equal(expected, map.HeightInTiles);

        Assert.False(map.BlocksPlayer(8, 8));
        Assert.True(MapTestHelpers.IsWalkableTile(map.GetTileId(8, 8)));
    }

    [Fact]
    public void CreateDungeonMap_has_single_exit_three_chests_and_connectivity()
    {
        var result = MapData.CreateDungeonMap(16);
        var map = result.TileMap;

        Assert.Equal(1, MapTestHelpers.CountTilesWithId(map, 4));
        Assert.Equal(3, MapTestHelpers.CountTilesWithId(map, 3));

        // Полная связность пола не гарантируется генератором; важно, чтобы выход был достижим из старта.
        var exitCell = MapTestHelpers.FindFirstTileWithId(map, 4);
        Assert.NotNull(exitCell);
        Assert.True(MapTestHelpers.IsReachableForPlayer(map, 8, 8, exitCell.Value.x, exitCell.Value.y));

        Assert.Equal(result.ExitRoomIndexX, map.ExitRoomIndexX);
        Assert.Equal(result.ExitRoomIndexY, map.ExitRoomIndexY);
        Assert.InRange(map.ExitRoomIndexX, 0, 2);
        Assert.InRange(map.ExitRoomIndexY, 0, 2);
    }

    [Fact]
    public void CreateDungeonMap_easy_produces_smaller_grid_than_hard()
    {
        var easy = MapData.CreateDungeonMap(16, MapDifficulty.Easy).TileMap;
        var hard = MapData.CreateDungeonMap(16, MapDifficulty.Hard).TileMap;
        Assert.True(easy.WidthInTiles < hard.WidthInTiles);
        Assert.Equal(21, easy.WidthInTiles);
        Assert.Equal(58, hard.WidthInTiles);
        Assert.Equal(2, easy.RoomsPerSide);
        Assert.Equal(3, hard.RoomsPerSide);
    }

    [Fact]
    public void CreateDungeonMap_easy_has_exit_reachable_and_three_chests()
    {
        var result = MapData.CreateDungeonMap(16, MapDifficulty.Easy);
        var map = result.TileMap;
        Assert.Equal(3, MapTestHelpers.CountTilesWithId(map, 3));
        Assert.Equal(1, MapTestHelpers.CountTilesWithId(map, 4));
        var exitCell = MapTestHelpers.FindFirstTileWithId(map, 4);
        Assert.NotNull(exitCell);
        Assert.True(MapTestHelpers.IsReachableForPlayer(map, 4, 4, exitCell.Value.x, exitCell.Value.y));
    }

    [Fact]
    public void CreateDungeonMap_places_three_chests_in_three_distinct_rooms()
    {
        var map = MapData.CreateDungeonMap(16).TileMap;
        var rooms = new System.Collections.Generic.HashSet<(int rx, int ry)>();

        for (int y = 0; y < map.HeightInTiles; y++)
        {
            for (int x = 0; x < map.WidthInTiles; x++)
            {
                if (map.GetTileId(x, y) != 3)
                    continue;
                TileMap.GetRoomIndexForTile(x, y, map.RoomStepTiles, map.RoomsPerSide, out int rx, out int ry);
                rooms.Add((rx, ry));
            }
        }

        Assert.Equal(3, rooms.Count);
        Assert.Equal(3, MapTestHelpers.CountTilesWithId(map, 3));
    }

    [Fact]
    public void DungeonMapResult_tileMap_is_not_null()
    {
        var result = MapData.CreateDungeonMap(8);
        Assert.NotNull(result.TileMap);
    }
}
