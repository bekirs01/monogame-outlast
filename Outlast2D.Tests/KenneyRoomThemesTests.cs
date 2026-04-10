using Outlast2D;
using Xunit;

namespace Outlast2D.Tests;

public class KenneyRoomThemesTests
{
    [Fact]
    public void RoomPairs_has_nine_entries()
    {
        Assert.Equal(9, KenneyRoomThemes.RoomPairs.Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void FileName_appends_png(int index)
    {
        var (floor, _) = KenneyRoomThemes.RoomPairs[index];
        Assert.EndsWith(".png", KenneyRoomThemes.FileName(floor));
        Assert.Equal(floor + ".png", KenneyRoomThemes.FileName(floor));
    }

    [Fact]
    public void RoomTheme_indices_cover_zero_to_eight()
    {
        for (int ry = 0; ry < 3; ry++)
        {
            for (int rx = 0; rx < 3; rx++)
            {
                int idx = TileMap.RoomThemeIndex(rx, ry);
                Assert.InRange(idx, 0, 8);
            }
        }
    }
}
