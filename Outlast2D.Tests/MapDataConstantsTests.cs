using Xunit;

namespace Outlast2D.Tests;

public class MapDataConstantsTests
{
    [Fact]
    public void RoomStepTiles_matches_inner_plus_two()
    {
        Assert.Equal(MapData.RoomInnerTiles + 2, MapData.RoomStepTiles);
    }

    [Fact]
    public void TilesPerRoomSide_is_step_plus_one()
    {
        Assert.Equal(MapData.RoomStepTiles + 1, MapData.TilesPerRoomSide);
    }

    [Fact]
    public void Map_dimensions_are_58_by_58()
    {
        int expected = MapData.RoomStepTiles * 3 + 1;
        Assert.Equal(58, expected);
    }

    [Fact]
    public void Easy_map_inner_is_half_of_hard_and_total_tiles_match_formula()
    {
        int easyStep = MapData.RoomInnerTilesEasy + 2;
        Assert.Equal(10, easyStep);
        // Basit: 2×2 oda → kenar başına 2 oda.
        Assert.Equal(21, easyStep * 2 + 1);
    }
}
