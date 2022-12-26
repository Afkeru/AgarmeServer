namespace AgarmeServer.HKObj
{
    public interface IPoint
    {
        double X { get; set; }
        double Y { get; set; }
        //计算到目标点的距离
        double CalDistance(IPoint value);
        //求模长
        double CalModule();
    }
}
