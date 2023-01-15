using System;
using System.Drawing;
using AgarmeServer.HKObj;
using AgarmeServer.Map;

namespace AgarmeServer.Entity
{
    public interface ICell
    {
        double X { get; set; }//横坐标
        double Y { get; set; }//纵坐标
        double R { get; set; }//半径
        double Transverse { get; set; }//横向移动距离
        double Longitudinal { get; set; }//纵向移动距离
        double Xd { get; set; }
        double Yd { get; set; }
        double Size { get; set; }//质量
        double Speed { get; set; }//移动速度
        HKColor Color { get; set; }//颜色
        HKPoint Cell_Point { get; }//返回细胞坐标
        byte Type { get; set; }//细胞类型
        uint Id { get; set; }//细胞身份证
        bool Deleted { get; set; }//细胞是否删除
        string Name { get; set; }//细胞名称

        void CollideWith(Cell Des);//和指定细胞碰撞
        void MoveTo_ATan(HKPoint Des, double speed);//向目标点移动
        void MoveTo(HKPoint Des, double speed, ref double tran, ref double longi);
        void SetID();//为该细胞设置ID身份证
        void ReturnID();//为该细胞设置ID身份证
        void SetRandomColor(byte methond);//设置随机颜色
        void MonitorBorderCollide();//检查与地图边界碰撞
        void Eat(Cell eated);//吞噬细胞
        double Distance(HKPoint Des);//取细胞到定点距离
        bool InRect(RectangleF rect);//细胞是否在指定矩形边界内
        bool InMap();//细胞是否在地图边界内
        bool IsCollideWith(Cell Des);//细胞是否与指定细胞发生碰撞
    }
}
