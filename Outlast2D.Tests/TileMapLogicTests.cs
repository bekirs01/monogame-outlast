using Outlast2D;
using Xunit;

namespace Outlast2D.Tests;

public class TileMapLogicTests
{
    [Fact]
    public void BlocksPlayer_blocks_wall_and_obstacle_only()
    {
        var g = new int[5, 5];
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                g[x, y] = 0;
        g[2, 2] = 1;
        g[3, 3] = 5;

        var map = new TileMap(g, 16, 0, 0);

        Assert.True(map.BlocksPlayer(2, 2));
        Assert.True(map.BlocksPlayer(3, 3));
        Assert.False(map.BlocksPlayer(0, 0));
        Assert.False(map.BlocksPlayer(1, 1));
    }

    [Fact]
    public void BlocksPlayer_blocks_exit_tile_until_enough_keys()
    {
        var g = new int[5, 5];
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                g[x, y] = 0;
        g[2, 2] = 4;

        var map = new TileMap(g, 16, 0, 0);

        Assert.True(map.BlocksPlayer(2, 2, 0));
        Assert.True(map.BlocksPlayer(2, 2, 2));
        Assert.False(map.BlocksPlayer(2, 2, 3));
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(18, 0, 0, 0)]
    [InlineData(19, 0, 1, 0)]
    [InlineData(37, 18, 1, 0)]
    [InlineData(38, 38, 2, 2)]
    public void GetRoomIndexForTile_clamps_to_three_by_three(int gx, int gy, int ex, int ey)
    {
        TileMap.GetRoomIndexForTile(gx, gy, out int rx, out int ry);
        Assert.Equal(ex, rx);
        Assert.Equal(ey, ry);
    }

    [Fact]
    public void ExitRoom_indices_match_constructor()
    {
        var g = new int[10, 10];
        g[0, 0] = 0;
        var map = new TileMap(g, 8, 2, 1);
        Assert.Equal(2, map.ExitRoomIndexX);
        Assert.Equal(1, map.ExitRoomIndexY);
    }

    [Fact]
    public void Pixel_dimensions_scale_with_tile_size()
    {
        var g = new int[4, 4];
        var map = new TileMap(g, 24, 0, 0);
        Assert.Equal(96, map.WidthPixels);
        Assert.Equal(96, map.HeightPixels);
    }
}
