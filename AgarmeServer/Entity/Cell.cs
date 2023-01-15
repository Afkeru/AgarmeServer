using AgarmeServer.HKObj;
using AgarmeServer.Map;
using AgarmeServer.Network;
using AgarmeServer.Others;
using AgarmeServer.Zeroer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AgarmeServer.Entity
{
    public class Cell : ICell, IQuadItem,IComparable<Cell>
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
        public HKPoint Cell_Point { get => new HKPoint(X,Y);set { X = value.X ; Y=value.Y ; Cell_Point = new HKPoint(X, this.Y); } }
        public RectangleF Range { get => new RectangleF((float)(x - R), (float)(y - R), (float)R * 2, (float)R * 2);  }

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

        public void Eat(Cell eated)
        {
            size += eated.size;
            eated.deleted = true;
            eated.EatenBy = this;
            switch (eated.Type)
            {
                case Constant.TYPE_PLAYER:
                    {
                        Player player = eated as Player;
                        if (!player.Client.OwnCells.ContainsKey(player.Id))
                            player.Client.OwnCells.Add(player.Id, player);
                        break;
                    }
                case Constant.TYPE_BOT:
                    {
                        Minion minion = eated as Minion;
                        if (!minion.Client.OwnCells.ContainsKey(minion.Id))
                            minion.Client.OwnCells.Add(minion.Id, minion);
                        break;
                    }
            }
        }

        public void CountInertia(ref double x, ref double y, ref double xxd, ref double yyd, double rate)
        {
            if (Math.Abs(xxd) is >= 0.01)
            {
                var delta_x = xxd * rate;
                x += delta_x;
                xxd = delta_x;
            }

            if (Math.Abs(yyd) >= 0.01)
            {
                var delta_y = yyd * rate;
                y += delta_y;
                yyd = delta_y;
            }
        }

        public void CountComponent(ref double x, ref double y, ref double xxd, ref double yyd, double accumulation)
        {
            if (Math.Abs(xxd) is >= 0.01)
            {
                var delta_x = xxd * accumulation;
                x += delta_x;
                xxd -= delta_x;
            }

            if (Math.Abs(yyd) >= 0.01)
            {
                var delta_y = yyd * accumulation;
                y += delta_y;
                yyd -= delta_y;
            }
        }
        //求细胞与指定点的连线与细胞的交点
        public HKPoint CellPoint(HKPoint Des,double constant=1)
        {
            var vec = new HKPoint();
            if (Des.Equals(Cell_Point)) 
                return vec;
            var DeltaX = Des.X - x;
            var DeltaY = Des.Y - y;
            var angle = Math.Atan2(DeltaY, DeltaX);//求出夹角的弧度
            if (double.IsNaN(angle)) return vec;
            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            vec.X = x + cos * r* constant;
            vec.Y = y + sin * r* constant;
            return vec;
        }

        #region 移动函数

        public void MoveTo_ATan(HKPoint Des, double speed)
        {
            if (Des.Equals(Cell_Point))
                return;
            var angle = Math.Atan2((Des.Y - y), (Des.X - x));//求出夹角的弧度

            if(double.IsNaN(angle)) return;

            var sin=Math.Sin(angle);
            var cos= Math.Cos(angle);
            var distance = Distance(Des);
            var radius = Math.Min(speed,distance);

            x += cos * radius;
            y += sin * radius;
        }

        public void MoveTo_ATan_Distance(HKPoint Des,double move_distance)
        {
            if (Des.Equals(Cell_Point))
                return;
            var angle = Math.Atan2((Des.Y - y), (Des.X - x));//求出夹角的弧度

            if (double.IsNaN(angle)) return;

            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            var radius = move_distance;

            x += cos * radius;
            y += sin * radius;
        }

        public void MoveTo_ATan_Distance(HKPoint Des, double move_distance, ref double trans, ref double longi)
        {
            if (Des.Equals(Cell_Point))
                return;
            var angle = Math.Atan2((Des.Y - y), (Des.X - x));//求出夹角的弧度

            if (double.IsNaN(angle)) return;

            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            var radius = move_distance;

            trans += cos * radius;
            longi += sin * radius;
        }

        public void MoveTo_ATan_Distance(double angle, double move_distance)
        {
            if (double.IsNaN(angle)) return;

            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            var radius = move_distance;

            x += cos * radius;
            y += sin * radius;
        }

        public void MoveTo_ATan_Distance(double angle, double move_distance,ref double trans,ref double longi)
        {
            if (double.IsNaN(angle)) return;

            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            var radius = move_distance;

            trans += cos * radius;
            longi += sin * radius;
        }

        public void MoveTo_Distance(HKPoint Des,double move_distance, ref double tran, ref double longi)
        {
            if (Des.Equals(Cell_Point))
                return;
            var angle = Math.Atan2((Des.Y - y), (Des.X - x));//求出夹角的弧度

            if (double.IsNaN(angle)) return;

            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            var radius = move_distance;

            tran += cos * radius;
            longi += sin * radius;
        }

        public void MoveTo(HKPoint Des, double speed, ref double tran, ref double longi)
        {
            if (Des.Equals(Cell_Point))
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
            var radius = speed;

            tran += cos * radius;
            longi += sin * radius;
        }
        #endregion

        public HKPoint CalDeviation(HKPoint Des, double fuzzyDiviation, double para_r)
        {
            var ret = new HKPoint();

            if (Des.Equals(this.Cell_Point)) return ret;
            var angle = Math.Atan2((Des.Y - y), (Des.X - x));//求出夹角的弧度

            if (double.IsNaN(angle)) return ret;

            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            var distance = Distance(Des);

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
                        color_index = 0;
                        color = HKColor.Rand();
                        return;
                    }
                case 1:
                    {
                        color_index =HKRand.Byte(0, (byte)(HKColor.colorTable.Length - 1));
                        color=HKColor.colorTable[color_index];
                        return;
                    }
            }
        }

        public void CollideWith(Cell Des)
        {
            switch (ServerConfig.PlayerCollisionNumber)
            {
                case 0://小鸟碰撞算法
                    {
                        var vec = Des.Cell_Point - Cell_Point;
                        var d = vec.CalModule();// distance from cell1 to cell2
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

                        break;
                    }
                case 1://Blob标准碰撞算法(效果不错)
                    {
                        var vec = Des.Cell_Point - Cell_Point;
                        var d = vec.CalModule();// distance from cell1 to cell2
                        if (d <= 0) return;
                        var invd = 1 / d;

                        // normal
                        var nx = vec.X * invd;
                        var ny = vec.Y * invd;

                        // body penetration distance
                        var penetration = r + Des.r - d;
                        if (penetration <= 0) return;

                        // penetration vector = penetration * normal
                        var px = penetration * nx;
                        var py = penetration * ny;

                        // body impulse
                        var totalMass = size + Des.size;
                        if (totalMass <= 0) return;
                        var invTotalMass = 1 / totalMass;
                        var impulse1 = Des.size * invTotalMass;
                        var impulse2 =size * invTotalMass;

                        x-= px * impulse1;
                        y-= py * impulse1;
                        Des.x += px * impulse2;
                        Des.y += py * impulse2;


                        break;
                    }
                case 2://Ogar2标准碰撞算法
                    {
                        if (id == Des.id)
                            break;
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
                case 3://AgarOss碰撞算法
                    {
                        if (id == Des.id)
                            break;
                        var vec = Cell_Point - Des.Cell_Point;
                        var dist = vec.CalModule();
                        var r_ = r + Des.r;
                        if (dist is 0)
                        {
                            dist = r_ - 1.0;
                            vec = new HKPoint(r, 0.0);
                        }
                        double penetration = r_ - dist;
                        HKPoint normal = vec / dist;
                        vec = normal * penetration;

                        // Get impulses
                        var impulseSum = Size + Des.Size;

                        // Cell has infinite mass, do not move
                        if (impulseSum is 0) return;

                        // Momentum
                        vec /= impulseSum;

                        // push-pull them apart based off their mass    
                        x += vec.X * Des.Size*0.6;
                        y += vec.Y * Des.Size*0.6;
                        Des.x -= vec.X * Size * 0.6;
                        Des.y -= vec.Y *Size * 0.6;

                        // (experimental) resolving penetration
                        var percent = 0.0015; // Percentage to move (higher= more jitter, less overlap)
                        var slop = 0.001; // If penetration is less than slop value then don't correct

                        var correction = normal * percent * (Math.Max(penetration - slop, 0.0) / impulseSum);

                        x += correction.X * Size;
                        y += correction.Y * Size;
                        Des.x -= correction.X * Des.Size;
                        Des.y -= correction.Y * Des.Size;
                        break;
                    }
                case 4://Agare双向推动
                    {
                        var vec = Des.Cell_Point - Cell_Point;
                        var d = vec.CalModule();// distance from cell1 to cell2
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
                        var size_reciprocal = ServerConfig.PlayerCollisionConstant / (size + Des.size) * m;
                        var move1 = size * size_reciprocal;
                        var move2 = Des.size * size_reciprocal;
                        x -= dx * move2;
                        y -= dy * move2;
                        Des.x += dx * move1;
                        Des.y += dy * move1;
                        break;
                    }
            }
        }

        public int CompareTo(Cell other)
        {
            if (size > other.size)
                return 1;
            else if (size == other.size)
                return 0;
            else
                return -1;
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
        public virtual void PushIntoList(List<Cell> CellList, QuadTree<Cell> Tree) { Tree.Insert(this); CellList.Add(this);  }
        public void ReturnID() { IdGenerator.Return((id - 1)); }
        public void Print() { Console.WriteLine(this); }
        public void SetID() { this.id = (IdGenerator.Rent() + 1);/*id = PresentMaxId++; */}
        private bool simpleCollide(Cell other, double collisionDist) => Math.Abs(x - other.x) < (2 * collisionDist) && Math.Abs(y - other.y) < (2 * collisionDist);
        public void Deviation(HKPoint Des, double fuzzyDiviation, double para_r = 0) { MoveTo_ATan(Des, para_r + fuzzyDiviation); }


    }
}
