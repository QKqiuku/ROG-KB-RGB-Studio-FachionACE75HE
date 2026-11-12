using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RSS_II_RGB.Core.Effects.Layers;

namespace RSS_II_RGB.App;

internal sealed partial class PresetItem : ObservableObject
{
    public EffectChoice Effect { get; }
    public string Label => L.EffectName(Effect);
    public IRelayCommand SelectCommand { get; }
    [ObservableProperty] private bool _isSelected;

    public PresetItem(EffectChoice effect, Action<EffectChoice> select)
    {
        Effect = effect;
        SelectCommand = new RelayCommand(() => select(effect));
    }
}

internal static class PresetCatalog
{
    public static readonly EffectChoice[] Regular = [
        EffectChoice.Solid, EffectChoice.Breathing, EffectChoice.Rainbow,
        EffectChoice.KeyTrigger, EffectChoice.StarField, EffectChoice.Wave,
        EffectChoice.SineWave,
        EffectChoice.Aurora, EffectChoice.Ocean, EffectChoice.Nebula, EffectChoice.Fire, EffectChoice.Candle, EffectChoice.Meteor, EffectChoice.Radar, EffectChoice.Heartbeat, EffectChoice.Checker, EffectChoice.Snake, EffectChoice.ScanBand, EffectChoice.Wipe, EffectChoice.Marble, EffectChoice.Silk, EffectChoice.Dna, EffectChoice.Sunset, EffectChoice.RainDrops, EffectChoice.Lava, EffectChoice.Lightning, EffectChoice.NeonPulse, EffectChoice.Marquee, EffectChoice.LakePixels, EffectChoice.LakeRipples, EffectChoice.LakeSurge, EffectChoice.PerimeterFlow, EffectChoice.RotatingRing, EffectChoice.DoubleBeacon, EffectChoice.SineSectors, EffectChoice.WindmillCycle, EffectChoice.Caustics, EffectChoice.Sun, EffectChoice.Sakura, EffectChoice.IceCrystal, EffectChoice.AuroraRipples, EffectChoice.Off
    ];
    public static readonly EffectChoice[] Animations = [EffectChoice.BrickBreaker, EffectChoice.CountdownFireworks];
    public static readonly EffectChoice[] Music = [EffectChoice.MusicSpectrumBars, EffectChoice.MusicBeatFlash, EffectChoice.MusicClassicEqualizer, EffectChoice.MusicMirrorSpectrum, EffectChoice.MusicNeonSpectrum, EffectChoice.MusicSoundWave, EffectChoice.MusicRhythmOrbit, EffectChoice.MusicShockwave, EffectChoice.MusicEnergyCore, EffectChoice.MusicBidirectionalSpectrum, EffectChoice.MusicRhythmFire];
    public static readonly EffectChoice[] All = [.. Regular, .. Animations, .. Music, EffectChoice.Gif, EffectChoice.Atk];
    public static MusicKind? MusicFor(EffectChoice effect) => !IsRemoved(effect) && effect >= EffectChoice.MusicSpectrumBars && effect <= EffectChoice.MusicRadialSpectrum
        ? (MusicKind)((int)effect - (int)EffectChoice.MusicSpectrumBars) : null;
    public static bool MusicHasSpeed(EffectChoice effect, bool rainbow) => MusicFor(effect) is { } kind &&
        (kind is MusicKind.EnergyCore or MusicKind.SoundWave or MusicKind.BeatFlash or MusicKind.SpectrumWaterfall or MusicKind.RhythmOrbit or MusicKind.Shockwave
        or MusicKind.BeatSparks or MusicKind.BeatRipples or MusicKind.BeatSweep or MusicKind.RhythmStars
        or MusicKind.BeatChecker or MusicKind.Party or MusicKind.MusicPuddles or MusicKind.RainbowCorridor
        || rainbow && kind is (MusicKind.NeonSpectrum or MusicKind.FlowNeonSpectrum or MusicKind.VolumeBreathing or MusicKind.PlasmaLightning));
    public static bool MusicHasWidth(EffectChoice effect) => MusicFor(effect) is { } kind && kind is not
        (MusicKind.BeatFlash or MusicKind.VolumeBreathing or MusicKind.RhythmHeart or MusicKind.SpectrumColors
        or MusicKind.BeatSparks or MusicKind.RhythmStars or MusicKind.BeatChecker);
    public static bool MusicHasGeometry(EffectChoice effect) => MusicFor(effect) is { } kind && kind is not
        (MusicKind.EnergyCore or MusicKind.BeatFlash or MusicKind.VolumeBreathing or MusicKind.BeatSparks or MusicKind.RhythmStars);
    public static readonly EffectChoice[] KeyEffects = [EffectChoice.Ripple, EffectChoice.KeySpotlight, EffectChoice.KeyDarkRipple, EffectChoice.KeyDiamond, EffectChoice.KeyDye, EffectChoice.KeyLightning, EffectChoice.KeyFireworks];
    public static readonly EffectChoice[] KeyChoices = [EffectChoice.Reactive, .. KeyEffects];
    public static KeyEffectKind? KeyPatternFor(EffectChoice effect) => IsRemoved(effect)?null:effect switch
    {
        EffectChoice.Ripple => KeyEffectKind.Ripple,
        EffectChoice.KeySpotlight => KeyEffectKind.Spotlight,
        EffectChoice.KeyDarkRipple => KeyEffectKind.DarkRipple,
        EffectChoice.KeyDoubleRing => KeyEffectKind.DoubleRing,
        EffectChoice.KeyDiamond => KeyEffectKind.Diamond,
        EffectChoice.KeyDye => KeyEffectKind.Dye,
        EffectChoice.KeyHeart => KeyEffectKind.Heart,
        EffectChoice.KeyLightning => KeyEffectKind.Lightning,
        EffectChoice.KeyFireworks => KeyEffectKind.Fireworks,
        _ => null,
    };
    public static PatternKind? PatternFor(EffectChoice effect) => IsRemoved(effect) ? null : effect switch
    {
        EffectChoice.Solid => PatternKind.Solid,
        EffectChoice.Breathing => PatternKind.Breathing,
        EffectChoice.Rainbow => PatternKind.Rainbow,
        EffectChoice.KeyTrigger => PatternKind.PressFade,
        EffectChoice.Rain => PatternKind.Rain,
        EffectChoice.Ripple => PatternKind.Ripple,
        EffectChoice.StarField => PatternKind.Stars,
        EffectChoice.Wave => PatternKind.Wave,
        EffectChoice.SineWave => PatternKind.SineWave,
        EffectChoice.Waterfall => PatternKind.Waterfall,
        EffectChoice.Bloom => PatternKind.Bloom,
        EffectChoice.BrickBreaker => PatternKind.BrickBreaker,
        EffectChoice.CountdownFireworks => PatternKind.CountdownFireworks,
        EffectChoice.Vortex => PatternKind.Vortex,
        EffectChoice.Aurora => PatternKind.Aurora,
        EffectChoice.Ocean => PatternKind.Ocean,
        EffectChoice.Nebula => PatternKind.Nebula,
        EffectChoice.Fire => PatternKind.Fire,
        EffectChoice.Candle => PatternKind.Candle,
        EffectChoice.Meteor => PatternKind.Meteor,
        EffectChoice.Radar => PatternKind.Radar,
        EffectChoice.Heartbeat => PatternKind.Heartbeat,
        EffectChoice.Checker => PatternKind.Checker,
        EffectChoice.Snake => PatternKind.Snake,
        EffectChoice.ScanBand => PatternKind.ScanBand,
        EffectChoice.Wipe => PatternKind.Wipe,
        EffectChoice.Marble => PatternKind.Marble,
        EffectChoice.Silk => PatternKind.Silk,
        EffectChoice.Dna => PatternKind.Dna,
        EffectChoice.Sunset => PatternKind.Sunset,
        EffectChoice.RainDrops => PatternKind.RainDrops,
        EffectChoice.Lava => PatternKind.Lava,
        EffectChoice.Lightning => PatternKind.Lightning,
        EffectChoice.NeonPulse => PatternKind.NeonPulse,
        EffectChoice.Marquee => PatternKind.Marquee,
        EffectChoice.LakePixels => PatternKind.LakePixels,
        EffectChoice.LakeRipples => PatternKind.LakeRipples,
        EffectChoice.LakeSurge => PatternKind.LakeSurge,
        EffectChoice.PerimeterFlow => PatternKind.PerimeterFlow,
        EffectChoice.RotatingRing => PatternKind.RotatingRing,
        EffectChoice.DoubleBeacon => PatternKind.DoubleBeacon,
        EffectChoice.SineSectors => PatternKind.SineSectors,
        EffectChoice.WindmillCycle => PatternKind.WindmillCycle,
        EffectChoice.Caustics => PatternKind.Caustics,
        EffectChoice.Sun => PatternKind.Sun,
        EffectChoice.Sakura => PatternKind.Sakura,
        EffectChoice.IceCrystal => PatternKind.IceCrystal,
        EffectChoice.AuroraRipples => PatternKind.AuroraRipples,
        _ => null,
    };
    public static bool IsRemoved(EffectChoice effect) => effect is
        EffectChoice.Stream or EffectChoice.Trail or EffectChoice.Windmill or EffectChoice.SpiralRing or EffectChoice.Nailong or EffectChoice.MulticolorSineSectors or
        EffectChoice.KeyHeart or EffectChoice.KeyDoubleRing or EffectChoice.Vortex or EffectChoice.Plasma or EffectChoice.NeonBorder or EffectChoice.Fireflies or EffectChoice.Galaxy
        or EffectChoice.Tunnel or EffectChoice.Kaleidoscope or EffectChoice.Bubbles or EffectChoice.Diamonds
        or EffectChoice.Heart or EffectChoice.HeartRipple
        or EffectChoice.Monet or EffectChoice.Rain or EffectChoice.Waterfall or EffectChoice.Bloom
        or EffectChoice.MusicSoftSpectrum or EffectChoice.MusicFlowNeonSpectrum or EffectChoice.MusicBeatSparks or EffectChoice.MusicSpectrumWaterfall or EffectChoice.MusicBottomEnergyCore or EffectChoice.MusicRhythmHeart or EffectChoice.MusicSpectrumColors or EffectChoice.MusicSpectrumRing or EffectChoice.MusicBottomSpectrumRing or EffectChoice.MusicBassSurge or EffectChoice.MusicBeatSweep or EffectChoice.MusicRhythmStars or EffectChoice.MusicBeatChecker or EffectChoice.MusicWaveformBounce or EffectChoice.MusicCentralRhythm or EffectChoice.MusicRowBars or EffectChoice.MusicSoundDiffusion or EffectChoice.MusicHeartPierce or EffectChoice.MusicSLine or EffectChoice.MusicPlasmaLightning or EffectChoice.MusicParty or EffectChoice.MusicMusicPuddles or EffectChoice.MusicFlameEye or EffectChoice.MusicRadialSpectrum or EffectChoice.MusicVolumeBreathing or EffectChoice.MusicBeatRipples or EffectChoice.MusicRainbowCorridor;
    public static EffectChoice Available(EffectChoice effect) => effect==EffectChoice.MulticolorSineSectors?EffectChoice.SineSectors:IsRemoved(effect) ? effect >= EffectChoice.MusicSpectrumBars && effect <= EffectChoice.MusicRadialSpectrum
        ? EffectChoice.MusicSpectrumBars : EffectChoice.Rainbow : effect;
    public static void NormalizeZone(ZoneSetting zone)
    {
        if(zone.Effect==EffectChoice.MulticolorSineSectors)
            zone.Parameters??=new EffectSetting {Rainbow=true};
        zone.Effect=Available(zone.Effect);
    }
    public static bool HasGeometry(EffectChoice effect) => MusicFor(effect) is not null ? MusicHasGeometry(effect) : (PatternFor(effect) is not null || effect==EffectChoice.Monet) && effect is not
        (EffectChoice.Solid or EffectChoice.Breathing or EffectChoice.Rainbow or EffectChoice.KeyTrigger
        or EffectChoice.Ripple or EffectChoice.StarField or EffectChoice.Candle or EffectChoice.Heartbeat or EffectChoice.CountdownFireworks or EffectChoice.BrickBreaker);
    public static bool HasMotion(EffectChoice effect,bool rainbow)=>MusicFor(effect) is not null?MusicHasSpeed(effect,rainbow):effect is not EffectChoice.Off and not EffectChoice.Solid and not EffectChoice.Audio and not EffectChoice.Starlight;
    public static bool HasWidth(EffectChoice effect) => MusicFor(effect) is not null ? MusicHasWidth(effect) : effect is EffectChoice.Rainbow or EffectChoice.Wave or EffectChoice.Rain or EffectChoice.Ripple or EffectChoice.Stream or EffectChoice.Trail
        or EffectChoice.SineWave or EffectChoice.Windmill or EffectChoice.Bloom or EffectChoice.BrickBreaker or EffectChoice.CountdownFireworks or EffectChoice.Vortex or EffectChoice.Waterfall or EffectChoice.Meteor or EffectChoice.Radar or EffectChoice.Snake
        or EffectChoice.ScanBand or EffectChoice.RainDrops
        or EffectChoice.NeonPulse or EffectChoice.Dna or EffectChoice.Checker
        or EffectChoice.Aurora or EffectChoice.Ocean or EffectChoice.Fire or EffectChoice.Marble
        or EffectChoice.Monet or EffectChoice.Marquee or EffectChoice.LakePixels or EffectChoice.LakeRipples or EffectChoice.LakeSurge
        or EffectChoice.PerimeterFlow or EffectChoice.RotatingRing or EffectChoice.SpiralRing or EffectChoice.DoubleBeacon
        or EffectChoice.SineSectors or EffectChoice.MulticolorSineSectors or EffectChoice.WindmillCycle or EffectChoice.Caustics
        or EffectChoice.Sun or EffectChoice.Sakura or EffectChoice.IceCrystal or EffectChoice.AuroraRipples;
    public static bool HasDensity(EffectChoice effect) => effect is EffectChoice.MusicBeatSparks or EffectChoice.MusicRhythmStars or EffectChoice.MusicParty || effect is EffectChoice.Rain or EffectChoice.StarField or EffectChoice.Windmill or EffectChoice.Bloom or EffectChoice.CountdownFireworks or EffectChoice.RainDrops or EffectChoice.Sakura or EffectChoice.IceCrystal;
    public static EffectSetting DefaultSettings(EffectChoice effect)=>new() {Rainbow=DefaultRainbow(effect),ColorHex=effect==EffectChoice.SineSectors?"FF4FA8":"00FF66"};
    public static EffectSetting DefaultReactiveSettings()=>new() {ColorHex="00B4FF"};
    public static bool DefaultRainbow(EffectChoice e) => e is not EffectChoice.SineSectors and not EffectChoice.Solid and not EffectChoice.Breathing
        and not EffectChoice.KeyTrigger and not EffectChoice.Ripple and not EffectChoice.Trail;
}
