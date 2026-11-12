using RSS_II_RGB.App;
using RSS_II_RGB.Core.Animation;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;
namespace ROG_KB_RGB_Studio.App.Tests;
public class GifPackageTests
{
    [Fact] public void ConverterDetectionDistinguishesFullAndLiteFolders()
    {
        string root=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            Assert.Null(GifImportService.LocateHelper(root));
            string helper=Path.Combine(root,"gifbakehost");Directory.CreateDirectory(helper);
            string exe=Path.Combine(helper,"RSS_II_RGB.GifBakeHost.exe");File.WriteAllBytes(exe,[]);
            Assert.Equal(exe,GifImportService.LocateHelper(root));File.Delete(exe);Assert.Null(GifImportService.LocateHelper(root));
        }
        finally{Directory.Delete(root,true);}
    }
    [Fact] public void ConvertedAnimationPlaysWithoutAnyConversionProcess()
    {
        var layout=Falchion75Layout.Profile;
        var anim=new KbAnim(layout.Cols,layout.Rows,[new KbAnim.Frame(100,Enumerable.Repeat(new Rgb(255,0,0),layout.Cols*layout.Rows).ToArray())]);
        var pixels=new Rgb[layout.LedCount];new GifLayer("saved",anim,KeyMask.All).Render(pixels,new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],layout));
        Assert.All(pixels,p=>Assert.Equal(new Rgb(255,0,0),p));
    }
}
