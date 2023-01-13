using AgarmeServer.Entity;
using AgarmeServer.HKObj;
using AgarmeServer.Others;
using AgarmeServer.Map;
using System.Drawing;

namespace AgarmeServer.Client
{
    public abstract class IClient
    {
        public string Name { set; get; } = "";//名称
        public double Mass { set; get; } = 0;//所有细胞质量和
        public HKPoint DeathLoc { set; get; } = new HKPoint();//死亡坐标
        public HKPoint LastMouceLoc { set; get; }//上一次的鼠标坐标
        public int[] ArrayCount = new int[5];//记录发送到客户端出去的各个细胞类型的数量
        public RectangleF NewRect { set; get; }//新视野
        public RectangleF OldRect { set; get; }//旧视野
        public double SightWidth { set; get; }//视野宽度
        public double SightHeight { set; get; }//视野高度
        public double AverageViewX;//平均中心横坐标
        public double AverageViewY;//平均中心纵坐标
        public double ViewX { set; get; }//所有细胞横坐标之和
        public double ViewY { set; get; }//所有细胞纵坐标之和
        public double MergeTick { set; get; }//融合计时
        public int SplitCount { get; set; }//用于发送的细胞总数量
        public int SplitAttempts { get; set; }//按下分裂的次数
        public int LastSplitCount { get; set; }//上一次分裂数量
        public int SpaceCount { get; set; }//上一次分裂数量
        public bool Deleted { get; set; }//是否将被删除
        public bool Focus { get; set; }//是否有焦点
        public bool Die { get; set; } = true;//是否死亡
        public HKRectD ViewArea { set; get; } = new HKRectD(0, 0, 0, 0);
        public Dictionary<uint, Cell> OwnCells = new Dictionary<uint, Cell>();

        //指定算法生成视野宽度和高度
        public void SetSightWH(MinionClient minion)
        {
            //var s = Math.Pow(Mass,ServerConfig.PlayerMassZoom)+Math.Min(SplitCount,32) * ServerConfig.PlayerViewZoom;
            //var s = Math.Pow(Mass * 1.5, ServerConfig.PlayerMassZoom) + Math.Min(SplitCount, 64) * ServerConfig.PlayerViewZoom;
            var s = 0d;
            var l = minion is null? OwnCells.Count : OwnCells.Count+ minion.OwnCells.Count;

            s = Math.Pow(Math.Min(64/(s), l), 0.4);

            var width = ServerConfig.ViewWidth * s / 2  * 1 ;
            var height = ServerConfig.ViewHeight * s / 2 * 1 ;

            ViewArea.X = AverageViewX-width*0.5;
            ViewArea.Y = AverageViewY-height*0.5;

            ViewArea.Right = ViewArea.X+ width;
            ViewArea.Bottom = ViewArea.Y+ height;
        }

        public double CalViewConstant(double size) =>
            size switch
            {
                > 1 => Math.Pow(Mass*1.5, ServerConfig.PlayerMassZoom) + Math.Min(SplitCount, 32) * ServerConfig.PlayerViewZoom,
                _ => 0
            };

        //是否在观战视野矩形内
        public bool SetSpectateSight(Cell val)
        {
            double s, a, b, r;
            bool InSight = false;
            r = Math.Sqrt(3000 / Constant.PI_D);
            s = Math.Pow(3000 * ServerConfig.PlayerViewZoom, ServerConfig.PlayerMassZoom);
            a = ServerConfig.SpectateWidth * s;
            b = ServerConfig.SpectateHeight * s;
            InSight = val.X - val.R > AverageViewX - a && val.X < AverageViewX + a - val.R && val.Y > AverageViewY - b + val.R && val.Y < AverageViewY + b - val.R;
            return InSight;
        }

        //取观战视野矩形
        public RectangleF GetSpectateRect()
        {
            double s, a, b;
            s = Math.Pow(3000 * ServerConfig.PlayerViewZoom, ServerConfig.PlayerMassZoom);
            a = ServerConfig.SpectateWidth * s;
            b = ServerConfig.SpectateHeight * s;
            return new RectangleF((float)(AverageViewX - a), (float)(AverageViewY - b), (float)(AverageViewX + a), (float)(AverageViewY + b));
        }
        public static PlayerClient SearchPlayer(World world, uint bt) => world.PlayerList.ContainsKey(bt) ? world.PlayerList[bt] : null;
        public bool CheckInRect(Cell val, HKRectD aa) => val.X >= aa.X && val.X <= aa.X + aa.Width && val.Y >= aa.Y && val.Y <= aa.Y + aa.Height;
        public bool CheckOutInSight(Cell val) => CheckInRect(val, ViewArea);
        public override string ToString() => $"ViewX：{this.ViewX},ViewY：{this.ViewY},AvgX:{this.AverageViewX},AvgY:{this.AverageViewY}" + $",SplitCount:{this.SplitCount}";
        public bool SplitCheck() => SpaceCount + LastSplitCount <= ServerConfig.PlayerSplitLimit;
        public int GetRestSplitCount() => ServerConfig.PlayerSplitLimit - SpaceCount - LastSplitCount;
        public RectangleF GetViewRect() => new RectangleF((float)(AverageViewX - SightWidth), (float)(AverageViewY - SightHeight), (float)(SightWidth * 2), (float)(SightHeight * 2));
        //public bool CheckAlive() => Mass is >0 && OwnCells.Count is > 0;
    }
}
