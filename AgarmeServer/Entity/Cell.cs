using AgarmeServer.HKObj;
using AgarmeServer.Map;
using AgarmeServer.Network;
using AgarmeServer.Others;
using AgarmeServer.Zeroer;
using System;
using System.Collections.Generic;
using System.Drawing;
namespace AgarmeServer.Entity
{
    public class Cell : ICell, IQuadStorable
    {
        public static IdPool IdGenerator = new IdPool();
        public static uint PresentMaxId = 1;
        public byte color_index;
        public Cell FatherCell;
        public double x;
        public double y;
        private double size;
        public double r;
        public double transverse;
        public double longitudinal;
        public double xd;
        public double yd;
        private double speed;
        private uint id;
        private byte type;
        private string name;
        private HKColor color;
        private bool deleted = false;
        public Cell EatenBy = null;

        public double X { get => x; set { x = value; } }
        public double Y { get => y; set { y = value; } }
        public double R { get { var radius = Math.Sqrt(this.size / Constant.PI_D);r = radius;return radius; } set { r = R; } }
        public double Size { get => size; set { size = value; } }
        public double Speed { get => speed; set { speed = value; } }
        public double Transverse { get=> transverse; set { transverse = value; } }
        public double Longitudinal { get => longitudinal; set { longitudinal = value; } }
        public double Xd { get => xd; set { xd = value; } }
        public double Yd { get => yd; set { yd = value; } }
        public HKColor Color { get => color; set { color = value; } }
        public byte Type { get => type; set { type = value; } }
        public uint Id { get => id;  set { id = value; } }
        public bool Deleted { get => deleted; set { deleted = value; } }
        public string Name {  get  => name;  set { name = value; } }
        public HKPoint Cell_Point { get => new HKPoint(this.X, this.Y); }
        public RectangleF Cell_Rect { get=> new RectangleF((float)(this.x - this.R), (float)(this.y - this.R), (float)this.R * 2, (float)this.R * 2); }
        public RectangleF Rect => Cell_Rect;

        public void MonitorBorderCollide()
        {
            if (ServerConfig.IsBoarderCollision)
            {
                //右边界
                if (X + r * (ServerConfig.BoarderCover) > ServerConfig.BoarderWidth)
                {
                    X = ServerConfig.BoarderWidth - r * (ServerConfig.BoarderCover);
                    if (ServerConfig.IsBoarderCollisionBounce) Transverse = -Transverse;
                }
                //左边界
                if (X - r * (1 - ServerConfig.BoarderCover) < 0)
                {
                    X = r * (1 - ServerConfig.BoarderCover);
                    if (ServerConfig.IsBoarderCollisionBounce) Transverse = -Transverse;
                }
                //上边界
                if (Y - r *(1- ServerConfig.BoarderCover) < 0)
                {
                    Y = r * (1 - ServerConfig.BoarderCover);
                    if (ServerConfig.IsBoarderCollisionBounce) Longitudinal = -Longitudinal;
                }
                //下边界
                if (Y + r * (ServerConfig.BoarderCover) > ServerConfig.BoarderHeight)
                {
                    Y = ServerConfig.BoarderHeight - r * (ServerConfig.BoarderCover);
                    if (ServerConfig.IsBoarderCollisionBounce) Longitudinal = -Longitudinal;
                }
            }
        }

        public void Eat(Cell eater, Cell eated,World world)
        {
            eater.size += eated.size;
            eated.deleted = true;
            world.quadtree.Remove(eated);
        }

        public void CountInertia(ref double x, ref double y, ref double xd, ref double yd, double rate)
        {
            if (xd >= 0.0001 || xd <= -0.0001)
            {
                x = x + xd;
                xd = xd * rate;
            }
            if (yd >= 0.0001 || yd <= -0.0001)
            {
                y = y + yd;
                yd = yd * rate;
            }
        }

        public void MoveTo_ATan(HKPoint Des, double speed)
        {
            if (Des.Equals(this.Cell_Point))
                return;
            var angle = Math.Atan2((Des.Y - y), (Des.X - x));//求出夹角的弧度

            if(double.IsNaN(angle)) return;

            var sin=Math.Sin(angle);
            var cos= Math.Cos(angle);
            var distance = Distance(Des);
            var radius = Math.Min(speed,distance);

            this.x += cos * radius;
            this.y += sin * radius;
        }

        public void MoveTo(HKPoint Des, double speed, ref double tran, ref double longi)
        {
            if (Des.Equals(this.Cell_Point))
                return;
            var angle = Math.Atan2((Des.Y - y), (Des.X - x));//求出夹角的弧度

            if (double.IsNaN(angle)) return;

            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            var distance = Distance(Des);
            var radius = Math.Min(speed, distance);

            tran += cos * radius;
            longi += sin * radius;
        }

        public void MoveTo(double angle, double speed, ref double tran, ref double longi)
        {
            if (angle is > 360 or < 0) return;

            if (double.IsNaN(angle)) return;

            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            var distance = 1.5;
            var radius = Math.Min(speed, distance);

            tran += cos * radius;
            longi += sin * radius;
        }

        public HKPoint CalDeviation(HKPoint Des, double fuzzyDiviation, double para_r)
        {
            var ret = new HKPoint();

            if (Des.Equals(this.Cell_Point)) return ret;
            var angle = Math.Atan2((Des.Y - y), (Des.X - x));//求出夹角的弧度

            if (double.IsNaN(angle)) return ret;

            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            var distance = Distance(Des);
            //var radius = Math.Min(speed, distance);

            ret.X = cos * (para_r + fuzzyDiviation);
            ret.Y = sin * (para_r + fuzzyDiviation);
            return ret;
        }

        public void SetRandomColor(byte methond)
        {
            //这里提供两种方式取得随机颜色，一种是由HKRandom类生成，另一种是直接从颜色码表中取
            switch (methond)
            {
                case 0:
                    {
                        this.color_index = 0;
                        this.color = HKColor.Rand();
                        return;
                    }
                case 1:
                    {
                        this.color_index =HKRand.Byte(0, (byte)(HKColor.colorTable.Length - 1));
                        this.color=HKColor.colorTable[this.color_index];
                        return;
                    }
            }
        }

        public void CollideWith(Cell Des)
        {
            switch (ServerConfig.PlayerCollisionNumber)
            {
                case 0:
                    {
                        HKPoint p = new HKPoint(this.x - Des.x, this.y - Des.y);
                        double size1 = this.size,size2 = Des.size;
                        double distance, temp3, tempMoveDistance, sin, cos, xd, yd;
                        distance = Distance(new HKPoint(Des.x, Des.y));
                        temp3 = this.r + Des.r - distance;
                        tempMoveDistance = temp3 * size2 / (size1 + size2) * 1.2;
                        sin = p.Y / distance;
                        cos = p.X / distance;
                        xd = cos * tempMoveDistance * ServerConfig.PlayerCollisionConstant;
                        yd = sin * tempMoveDistance * ServerConfig.PlayerCollisionConstant;
                        x += xd;
                        y += yd;
                        break;
                    }
                case 1:
                    {
                        var vec = Des.Cell_Point - Cell_Point;
                        var d = vec.CalModule();
                        var dx = vec.X;
                        var dy = vec.Y;
                        var m = r + Des.r - d;
                        if (m is <= 0) return;
                        if (d is 0)
                        {
                            d = 1;
                            dx = 1;
                            dy = 0;
                        }
                        else
                        {
                            dx /= d;
                            dy /= d;
                        }
                        var M = size + Des.size;
                        var aM = Des.size / M;
                        var bM = size / M;
                        x -= dx * Math.Min(m, r) * aM;
                        y -= dy * Math.Min(m, r) * aM;
                        Des.x += dx * Math.Min(m, Des.r) * bM;
                        Des.y += dy * Math.Min(m, Des.r) * bM;
                        break;
                    }
                case 2:
                    {
                        var vec =Cell_Point - Des.Cell_Point;
                        var d = vec.CalModule();
                        var dx = vec.X;
                        var dy = vec.Y;
                        var angle = Math.Atan2(dx, dy);
                        var speed = Math.Min(this.speed, d);
                        var x1 = x + (speed * Math.Sin(angle));
                        var y1= y+ (speed * Math.Cos(angle));

                        var collisionDist = r + Des.r;

                        var newDeltaY =y1 - Des.y ;
                        var newDeltaX =x1 - Des.x ;
                        var newAngle = Math.Atan2(newDeltaX, newDeltaY);

                        var move = ServerConfig.CollisionDeviationConstant*(collisionDist - d);

                        x +=(move * Math.Sin(newAngle));
                        y += (move * Math.Cos(newAngle));

                        break;
                    }
                case 3:
                    {
                        var vec =  Cell_Point- Des.Cell_Point;
                        double _r=r+ Des.r;
                        double dist = vec.CalModule();

                        if(dist is 0)
                        {
                            dist = _r - 1.0;
                            vec = new HKPoint(_r,0.0);
                        }

                        double penetration = _r - dist;
                        HKPoint normal = vec / dist;
                        vec = normal * penetration;
                        double impulseSum = size + Des.size;

                        if (impulseSum is 0) return;

                        vec /= impulseSum;

                        x += vec.X * size;
                        y += vec.Y * size;
                        Des.x-=vec.X*Des.size;
                        Des.y -= vec.Y * Des.size;

                        double percent = 0.0015;
                        double slop = 0.001;

                        HKPoint correction = normal * percent *( Math.Max(penetration - slop, 0.0) / impulseSum);
                        x+=correction.X * size;
                        y+=correction.Y * size;
                        Des.x-=correction.X*Des.size;
                        Des.y-=correction.Y*Des.size;

                        break;
                    }
            }
        }

        public string GetTypeStr() =>
            this.type switch
            {
                Constant.TYPE_FOOD=>"食物",
                Constant.TYPE_PLAYER => "玩家",
                Constant.TYPE_VIRUS => "病毒",
                Constant.TYPE_RED_VIRUS => "红色病毒",
                Constant.TYPE_EJECT => "吐球",
                Constant.TYPE_BOT => "人机",
                Constant.TYPE_ROBOT => "机器人",
                _=>"意外的类型"
            };
        public override string ToString()=> $"横坐标：{(int)this.x},纵坐标：{(int)this.y},Type:{this.GetTypeStr()},ID：{this.id},颜色：({this.color}),半径：{(int)this.R},质量：{(int)this.size}";
        public double Distance(HKPoint Des) => (new HKPoint(Des.X - this.x, Des.Y - this.y)).CalModule();
        public bool IsCollideWith(Cell Des) => Distance(Des.Cell_Point) <= (this.r + Des.r);
        public bool IsCollideWith(Cell Des,double distance) => distance <= (this.r + Des.r);
        public bool InRect(RectangleF rect) => this.x - r < rect.X && this.x + r > rect.X + rect.Width && this.y - r < rect.Y && this.y + r> rect.Y + rect.Height;
        public bool InMap() => this.x - r < 0 && this.x + r > ServerConfig.BoarderWidth && this.y - r < 0 && this.y + r > ServerConfig.BoarderHeight;
        public virtual void PushIntoList(List<Cell> CellList, QuadTree<Cell> Tree) { Tree.Add(this); CellList.Add(this);  }
        public void ReturnID() { IdGenerator.Return((uint)(this.id - 1)); }
        public void Print() { Console.WriteLine(this); }
        public void SetID() { /*this.id = (IdGenerator.Rent() + 1);*/id = PresentMaxId++; }
        public void Deviation(HKPoint Des, double fuzzyDiviation, double para_r = 0) { MoveTo_ATan(Des, para_r + fuzzyDiviation); }
    }
}
