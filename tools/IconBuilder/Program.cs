using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
// Original three-keycap mark, matching Assets/AppIcon.svg. This is a developer tool only.
string folder=Path.GetFullPath(args.Single());Directory.CreateDirectory(folder);
static GraphicsPath Rounded(float x,float y,float w,float h,float radius)
{
    var p=new GraphicsPath();float d=radius*2;
    p.AddArc(x,y,d,d,180,90);p.AddArc(x+w-d,y,d,d,270,90);p.AddArc(x+w-d,y+h-d,d,d,0,90);p.AddArc(x,y+h-d,d,d,90,90);p.CloseFigure();return p;
}
static Bitmap Draw(int size)
{
    var image=new Bitmap(size,size,PixelFormat.Format32bppArgb);using var g=Graphics.FromImage(image);
    g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.ScaleTransform(size/128f,size/128f);
    using(var path=Rounded(4,4,120,120,28)){using var fill=new SolidBrush(Color.FromArgb(11,16,29));using var stroke=new Pen(Color.FromArgb(41,52,76),2);g.FillPath(fill,path);g.DrawPath(stroke,path);}
    foreach(var k in new[]{(23f,43f,47f,Color.FromArgb(255,49,94)),(53f,24f,66f,Color.FromArgb(169,96,255)),(83f,36f,54f,Color.FromArgb(41,216,244))})
    {using var path=Rounded(k.Item1,k.Item2,22,k.Item3,7);using var brush=new SolidBrush(k.Item4);g.FillPath(brush,path);}
    using(var pen=new Pen(Color.FromArgb(220,255,255,255),3){StartCap=LineCap.Round,EndCap=LineCap.Round}){g.DrawLine(pen,30,54,38,54);g.DrawLine(pen,60,36,68,36);g.DrawLine(pen,90,47,98,47);}
    using(var pen=new Pen(Color.FromArgb(99,112,142),5){StartCap=LineCap.Round,EndCap=LineCap.Round})g.DrawLine(pen,27,102,101,102);
    return image;
}
int[] sizes=[16,24,32,48,64,128,256];var frames=new List<byte[]>();
foreach(int size in sizes){using var bitmap=Draw(size);using var stream=new MemoryStream();bitmap.Save(stream,ImageFormat.Png);frames.Add(stream.ToArray());if(size==256)bitmap.Save(Path.Combine(folder,"AppIcon.png"),ImageFormat.Png);}
using(var stream=File.Create(Path.Combine(folder,"Mainicon.ico")))
using(var writer=new BinaryWriter(stream))
{
    writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;
    for(int i=0;i<sizes.Length;i++){writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(frames[i].Length);writer.Write(offset);offset+=frames[i].Length;}
    foreach(var frame in frames)writer.Write(frame);
}
Console.WriteLine("Generated original icon PNG + seven-size ICO.");
