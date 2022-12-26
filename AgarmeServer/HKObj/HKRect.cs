namespace AgarmeServer.HKObj
{
    public class HKRectD
    {
        public double X;
        public double Y;
        public double Bottom;
        public double Right;
        public double Width { get=>Right-X; }
        public double Height { get=>Bottom-Y; }

        public HKRectD(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Right = x + width;
            Bottom = y + height;
        }

        public override string ToString() => $"Left：{X},Top：{Y},Right:{Right},Bottom:{Bottom}";

    }
}
