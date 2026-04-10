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
}
