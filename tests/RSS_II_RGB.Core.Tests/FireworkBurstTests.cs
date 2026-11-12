using RSS_II_RGB.Core.Effects.Layers;

namespace RSS_II_RGB.Core.Tests;

public class FireworkBurstTests
{
    [Fact]
    public void RocketRisesFromBottomToOrigin()
    {
        Assert.Equal(0, FireworkBurst.Light(.4, 1, .4, .3, -.01, 1, .3, 0));
        Assert.Equal(1, FireworkBurst.Light(.4, 1, .4, .3, 0, 1, .3, 0));
        Assert.Equal(1, FireworkBurst.Light(.4, .65, .4, .3, .15, 1, .3, 0), 12);
        Assert.True(FireworkBurst.Light(.4, .65, .4, .3, .15, 1, .3, 0) >
                    FireworkBurst.Light(.4, 1, .4, .3, .15, 1, .3, 0));
    }

    [Fact]
    public void BurstStartsAtOriginThenExpires()
    {
        Assert.Equal(1, FireworkBurst.Light(.4, .3, .4, .3, .3, 1, .3, 7));
        Assert.InRange(FireworkBurst.Light(.4, .3, .4, .3, .8, 1, .3, 7), 0, .71);
        Assert.Equal(0, FireworkBurst.Light(.4, .3, .4, .3, 2.1, 1, .3, 7));
    }

    [Fact]
    public void MovingOriginMovesTheWholeBurst()
    {
        Assert.Equal(FireworkBurst.Light(.3, .4, .2, .3, .8, 1.2, .6, 3),
                     FireworkBurst.Light(.6, .6, .5, .5, .8, 1.2, .6, 3), 12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(9)]
    public void ExtractedBloomPreservesCountdownParticleMath(int seed)
    {
        const double width = 1.2, density = .6, age = .9, ox = .35, oy = .4;
        double expansion = age - .3;
        int particles = 8 + (int)(density * 16);
        double hash = Math.Sin(seed * 7 * 12.9898 + 78.233) * 43758.5453;
        hash -= Math.Floor(hash);
        for (int row = 0; row <= 5; row++)
        for (int col = 0; col <= 14; col++)
        {
            double x = col / 14d, y = row / 5d, expected = 0;
            for (int p = 0; p < particles; p++)
            {
                double a = 2 * Math.PI * p / particles + hash * 2 * Math.PI;
                double px = ox + Math.Cos(a) * expansion * .28;
                double py = oy + Math.Sin(a) * expansion * .6 + expansion * expansion * .13;
                double distance = Math.Pow((x - px) * 14, 2) + Math.Pow((y - py) * 5, 2);
                expected = Math.Max(expected, Math.Exp(-distance / (.45 * width * width)) * Math.Max(0, 1 - expansion / 1.7));
            }
            Assert.Equal(expected, FireworkBurst.Light(x, y, ox, oy, age, width, density, seed));
        }
    }
}
