using Microsoft.Xna.Framework.Input;
using Outlast2D;
using Xunit;

namespace Outlast2D.Tests;

public class PlayerMovementTests
{
    [Fact]
    public void Constructor_sets_grid_position()
    {
        var p = new Player(3, 7);
        Assert.Equal(3, p.GridX);
        Assert.Equal(7, p.GridY);
    }

    [Fact]
    public void Update_moves_right_when_not_blocked()
    {
        var map = MapTestHelpers.CreateAllFloorMap(20, 20, 16);
        var p = new Player(5, 5);

        var kb = new KeyboardState(Keys.D);
        p.Update(kb, map, 0.2f, 0);

        Assert.Equal(6, p.GridX);
        Assert.Equal(5, p.GridY);
    }

    [Fact]
    public void Update_does_not_enter_wall_tile()
    {
        var g = new int[20, 20];
        for (int y = 0; y < 20; y++)
            for (int x = 0; x < 20; x++)
                g[x, y] = 0;
        g[6, 5] = 1;

        var map = new TileMap(g, 16, 0, 0);
        var p = new Player(5, 5);

        var kb = new KeyboardState(Keys.D);
        p.Update(kb, map, 0.2f, 0);

        Assert.Equal(5, p.GridX);
    }

}
