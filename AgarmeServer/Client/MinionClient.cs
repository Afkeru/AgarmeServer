using AgarmeServer.HKObj;

namespace AgarmeServer.Client
{
    public class MinionClient:IClient
    {
        public static uint MaxNumber = 1;
        public PlayerClient Parent = null;
        public uint Number { set; get; }//玩家的所属
        public void AllocateBT() { Number = MaxNumber++; }

        public MinionClient()
        {
            var _x = ServerConfig.BoarderWidth * 0.5d;
            var _y = ServerConfig.BoarderHeight * 0.5d;

            Focus = false;

            AverageViewX = HKRand.Double(0, ServerConfig.BoarderWidth);
            AverageViewY = HKRand.Double(0, ServerConfig.BoarderHeight);
        }
        public void OnDead() { Die = true; Parent.Tab = false; Focus = false; Mass = 0; SpaceCount = 0; SplitCount = 0; ViewX = 0; ViewY = 0; ViewArea = new HKRectD(0, 0, 0, 0); }
    }
}
