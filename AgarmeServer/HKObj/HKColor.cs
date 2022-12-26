using System.Text;
namespace AgarmeServer.HKObj
{
    public struct HKColor : IRgba<byte>
    {
        public static readonly uint[] colorTable = new uint[]
        {
            16777215U,4293982463U,4294634455U,4278255615U,4286578644U,4293984255U,4294309340U,4294960324U,4278190080U,4294962125U,
            4278190335U,4287245282U,4289014314U,4292786311U,4284456608U,4286578432U,4291979550U,4294934352U,4284782061U,4294965468U,
            4292613180U,4278255615U,4278190219U,4278225803U,4290283019U,4289309097U,4278215680U,4290623339U,4287299723U,4283788079U,4294937600U,
            4288230092U,4287299584U,4293498490U,4287609995U,4282924427U,4281290575U,4278243025U,4287889619U,4294907027U,4278239231U,4285098345U,4280193279U,
            4289864226U,4294966000U,4280453922U,4294902015U,4292664540U,4294506751U,4294956800U,4292519200U,4286611584U,4278222848U,4289593135U,
            4293984240U,4294928820U,4291648604U,4283105410U,4294967280U,4293977740U,4293322490U,4294963445U,4286381056U,4294965965U,4289583334U,4293951616U,
            4292935679U,4294638290U,4292072403U,4287688336U,4294948545U,4294942842U,4280332970U,4287090426U,4286023833U,4289774814U,4294967264U,4278255360U,4281519410U,
            4294635750U,4294902015U,4286578688U,4284927402U,4278190285U,4290401747U,4287852763U,4282168177U,4286277870U,4278254234U,4282962380U,4291237253U,
            4279834992U,4294311930U,4294960353U,4294960309U,4294958765U,4278190208U,4294833638U,4286611456U,4285238819U,
            4294944000U,4294919424U,4292505814U,4293847210U,4288215960U,4289720046U,4292571283U,4294963157U,4294957753U,4291659071U,4294951115U,4292714717U,
            4289781990U,4286578816U,4294901760U,4290547599U, 4282477025U,4287317267U,4294606962U,4294222944U,4281240407U,4294964718U,4288696877U, 4290822336U,4287090411U,
            4285160141U,4285563024U,4294966010U,4278255487U,4282811060U,4291998860U,4278222976U,4292394968U,4294927175U, 4282441936U,4293821166U,4294303411U,uint.MaxValue,4294309365U,4294967040U,4288335154U,4278255615U
        };
        public byte r, g, b, a;
        byte IRgba<byte>.A { get => a; set => a = value; }
        byte IRgb<byte>.R { get => r; set => r = value; }
        byte IRgb<byte>.G { get => g; set => g = value; }
        byte IRgb<byte>.B { get => b; set => b = value; }
        public static unsafe implicit operator int(HKColor c)//转换成整数型
        {
            int i;
            var p = (byte*)&i;
            *p++ = c.r;
            *p++ = c.g;
            *p++ = c.b;
            *p = c.a;
            return i;
        }
        public HKColor(byte r, byte g, byte b, byte a = byte.MaxValue)
        {
            this.r = r; this.g = g; this.b = b; this.a = a;
        }
        public HKColor(byte gray) : this(gray, gray, gray)
        {
        }
        public unsafe HKColor(int i)
        {
            var p = (byte*)&i;
            r = *p++;
            g = *p++;
            b = *p++;
            a = *p;
        }
        public unsafe HKColor(uint i)
        {
            var p = (byte*)&i;
            r = *p++;
            g = *p++;
            b = *p++;
            a = *p;
        }
        public static unsafe implicit operator uint(HKColor c)
        {
            uint i;
            var p = (byte*)&i;
            *p++ = c.r;
            *p++ = c.g;
            *p++ = c.b;
            *p = c.a;
            return i;
        }
        public static unsafe implicit operator HKColor(int c)=> new HKColor(c);
        public static unsafe implicit operator HKColor(uint c)=> new HKColor(c);
        private double Brightness
        {
            get => 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            //sb.Append("r");
            sb.Append(r.ToString());
            sb.Append(",");
            sb.Append(g.ToString());
            sb.Append(",");
            sb.Append(b.ToString());
            sb.Append(",");
            sb.Append(a.ToString());
            return sb.ToString();
        }
        public static HKColor Rand() => new HKColor(HKRand.Byte(0, 255), HKRand.Byte(0, 255), HKRand.Byte(0, 255),100);
    }
}
