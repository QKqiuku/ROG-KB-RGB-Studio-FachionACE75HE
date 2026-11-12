namespace RSS_II_RGB.Core.Effects.Layers;

/// <summary>The countdown and key-triggered fireworks share this normalized rocket and bloom.</summary>
public static class FireworkBurst
{
    public static double Light(double x, double y, double originX, double originY,
        double age, double width, double density, int seed)
    {
        if (age < 0) return 0;
        if (age < .3)
        {
            double rocketY = 1 - (1 - originY) * age / .3;
            return Math.Exp(-Math.Pow((x - originX) * 22 / width, 2) - Math.Pow((y - rocketY) * 7 / width, 2));
        }

        double expansion = age - .3, glow = 0;
        int particles = 8 + (int)(density * 16);
        double phaseHash = Math.Sin(seed * 7 * 12.9898 + 78.233) * 43758.5453;
        phaseHash -= Math.Floor(phaseHash);
        for (int p = 0; p < particles; p++)
        {
            double a = 2 * Math.PI * p / particles + phaseHash * 2 * Math.PI;
            double px = originX + Math.Cos(a) * expansion * .28;
            double py = originY + Math.Sin(a) * expansion * .6 + expansion * expansion * .13;
            double distance = Math.Pow((x - px) * 14, 2) + Math.Pow((y - py) * 5, 2);
            glow = Math.Max(glow, Math.Exp(-distance / (.45 * width * width)) * Math.Max(0, 1 - expansion / 1.7));
        }
        return glow;
    }
}
