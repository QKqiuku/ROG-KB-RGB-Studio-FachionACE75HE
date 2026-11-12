using System.Text.Json.Serialization;

namespace RSS_II_RGB.App;

/// <summary>The effects offered in the M1 UI. (Per-key/zone assignment is post-M1.)</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EffectChoice>))]
// Retired entries keep their numeric slots so saved effects can be migrated.
internal enum EffectChoice
{
    Off,
    Solid,
    Breathing,
    Rainbow,
    Wave,
    Reactive,
    CpuTemp,
    GpuTemp,
    Audio,
    Gif,
    Starlight,
    KeyTrigger, Rain, Ripple, StarField, Stream, Trail, SineWave, Windmill,
    Waterfall, Bloom, BrickBreaker, CountdownFireworks, Vortex,
    Atk,
    // Removed names retain their persisted values so old settings can be migrated.
    Aurora, Ocean, Plasma, Nebula, Fire, Candle, Meteor, Radar, Heartbeat, Checker, Snake, ScanBand, NeonBorder, Wipe, Marble, Silk, Dna, Sunset, Fireflies, RainDrops, Heart, HeartRipple, Galaxy, Tunnel, Kaleidoscope, Lava, Bubbles, Lightning, Diamonds, NeonPulse,
    Monet, Marquee, LakePixels, LakeRipples, LakeSurge, PerimeterFlow, RotatingRing, SpiralRing, DoubleBeacon, SineSectors, MulticolorSineSectors, WindmillCycle, Caustics, Sun, Sakura, IceCrystal, AuroraRipples, Nailong, KeySpotlight, KeyDarkRipple, KeyDoubleRing, KeyDiamond, KeyDye, KeyHeart, KeyLightning, KeyFireworks,
    MusicSpectrumBars, MusicBeatFlash, MusicClassicEqualizer, MusicMirrorSpectrum, MusicNeonSpectrum, MusicFlowNeonSpectrum, MusicSoftSpectrum, MusicSoundWave, MusicSpectrumWaterfall, MusicRhythmOrbit, MusicShockwave, MusicBeatSparks, MusicEnergyCore, MusicBottomEnergyCore, MusicVolumeBreathing, MusicRhythmHeart, MusicSpectrumColors, MusicBidirectionalSpectrum, MusicSpectrumRing, MusicBottomSpectrumRing, MusicBeatRipples, MusicBassSurge, MusicBeatSweep, MusicRhythmStars, MusicBeatChecker, MusicRhythmFire, MusicWaveformBounce, MusicCentralRhythm, MusicRowBars, MusicSoundDiffusion, MusicHeartPierce, MusicSLine, MusicPlasmaLightning, MusicParty, MusicRainbowCorridor, MusicMusicPuddles, MusicFlameEye, MusicRadialSpectrum,
}
