using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;

namespace RSS_II_RGB.App;

/// <summary>Native-resolution live field, computed off the UI and keyboard output threads.</summary>
public sealed class MusicCanvasView : Control
{
    public const int CanvasWidth=655,CanvasHeight=240;
    private readonly Rgb[] _pixels=new Rgb[CanvasWidth*CanvasHeight];
    private readonly byte[] _bytes=new byte[CanvasWidth*CanvasHeight*4];
    private readonly Stopwatch _clock=Stopwatch.StartNew();
    private readonly DispatcherTimer _timer=new() {Interval=TimeSpan.FromSeconds(1.0/30)};
    private CancellationTokenSource _lifetime=new();
    private WriteableBitmap? _bitmap;
    private MusicLayer? _layer;
    private MusicCanvasSettings? _settings;
    private bool _busy;
    public MusicCanvasView()
    {
        Width=MinWidth=MaxWidth=CanvasWidth;Height=MinHeight=MaxHeight=CanvasHeight;ClipToBounds=true;
        _timer.Tick+=OnTick;
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if(_lifetime.IsCancellationRequested) {_lifetime.Dispose();_lifetime=new();}
        _bitmap=new WriteableBitmap(new PixelSize(CanvasWidth,CanvasHeight),new Vector(96,96),PixelFormat.Bgra8888,AlphaFormat.Opaque);
        _timer.Start();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer.Stop();_lifetime.Cancel();_bitmap?.Dispose();_bitmap=null;_layer=null;_settings=null;
        base.OnDetachedFromVisualTree(e);
    }
    private async void OnTick(object? sender,EventArgs e)
    {
        if(_busy || !IsEffectivelyVisible || DataContext is not MainViewModel vm || vm.LiveMusicSettings is not { } settings) return;
        _busy=true;
        var ct=_lifetime.Token;
        try
        {
            _timer.Interval=TimeSpan.FromSeconds(1.0/Math.Min(30,vm.TargetFps));
            if(_layer is null || _settings?.Kind!=settings.Kind || _settings.Color!=settings.Color || _settings.Options!=settings.Options)
                _layer=new MusicLayer("continuous-preview",settings.Kind,new SensorState(),settings.Color,KeyMask.All,settings.Options);
            _settings=settings;
            var layer=_layer;
            var source=vm.LiveMusicAudio;
            var audio=source.IsFresh(Environment.TickCount64)?source:AudioFrame.Empty with {Generation=source.Generation};
            var elapsed=vm.LiveMusicTime??_clock.Elapsed;
            await Task.Run(()=>
            {
                var bounds=MusicLayer.CanvasBounds(settings.Kind);
                layer.RenderCanvas(_pixels,CanvasWidth,CanvasHeight,elapsed,audio,ct,bounds.Top,bounds.Bottom);
                float brightness=(float)Math.Clamp(settings.Brightness,0,1);
                for(int i=0;i<_pixels.Length;i++)
                {
                    var p=_pixels[i].Scale(brightness);int offset=i*4;
                    _bytes[offset]=p.B;_bytes[offset+1]=p.G;_bytes[offset+2]=p.R;_bytes[offset+3]=255;
                }
            },ct);
            if(ct.IsCancellationRequested || _bitmap is null || vm.LiveMusicSettings!=settings || vm.LiveMusicAudio.Generation!=audio.Generation) return;
            using(var target=_bitmap.Lock())
                for(int row=0;row<CanvasHeight;row++) Marshal.Copy(_bytes,row*CanvasWidth*4,target.Address+row*target.RowBytes,CanvasWidth*4);
            InvalidateVisual();
        }
        catch(OperationCanceledException) { }
        catch(Exception ex) {Debug.WriteLine($"Music canvas preview stopped: {ex}");_timer.Stop();}
        finally {_busy=false;}
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(Brushes.Black,new Rect(0,0,CanvasWidth,CanvasHeight));
        if(_bitmap is not null) context.DrawImage(_bitmap,new Rect(0,0,CanvasWidth,CanvasHeight));
    }
}
