using System;
using System.Drawing;
namespace AgarmeServer.HKObj
{
    public class HKPoint : IPoint
    {
        //内部私有变量
        private double x;
        private double y;

        //实现IPoint接口
        public double X { get { return x; } set { x = value; } }
        public double Y { get { return y; } set { y = value; } }

        //构造函数
        public HKPoint() { }
        public HKPoint(double x, double y) { this.x = x; this.y = y; }
        public HKPoint(HKPoint value) { this.x = value.x; this.y = value.y; }

        //运算符重载
        public static HKPoint operator +(HKPoint Val1, HKPoint Val2)=> new HKPoint(Val1.X + Val2.X, Val1.Y + Val2.Y);
        public static HKPoint operator -(HKPoint Val1, HKPoint Val2) => new HKPoint(Val1.X - Val2.X, Val1.Y - Val2.Y);
        public static HKPoint operator *(HKPoint Val1, HKPoint Val2) => new HKPoint(Val1.X * Val2.X, Val1.Y * Val2.Y);
        public static HKPoint operator *(HKPoint Val1, double Val2) => new HKPoint(Val1.X * Val2, Val1.Y * Val2);
        public static HKPoint operator /(HKPoint Val1, HKPoint Val2) => new HKPoint(Val1.X / Val2.X, Val1.Y / Val2.Y);
        public static HKPoint operator /(HKPoint Val1, double Val2) => new HKPoint(Val1.X / Val2, Val1.Y / Val2);


        //求该点的长度
        public double CalModule()=> Math.Sqrt(this.X * this.X + this.Y * this.Y);
        
        //计算该点到目标点的距离
        public double CalDistance(IPoint Des)=> new HKPoint(Des.X - this.X, Des.Y - this.Y).CalModule();
        
        //重写HKPoint的ToString函数
        public override string ToString() =>$"横坐标：{this.X},纵坐标：{this.Y}";
        
        //隐式转换成point类型(会丢失精度)
        public static unsafe implicit operator Point(HKPoint Des)=> new Point((int)Des.X, (int)Des.Y);

        //隐式转换成pointF类型(会丢失精度)
        public static unsafe implicit operator PointF(HKPoint Des) => new PointF((float)Des.X, (float)Des.Y);
    }
}
