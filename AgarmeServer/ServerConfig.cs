namespace AgarmeServer
{
    public class ServerConfig
    {
        //Server
        static public string ip = "127.0.0.1";//服务端ip
        static public ushort port = 6666;//端口
        public const string version = "V1.0.0";//服务端版本号
        public const ushort version_code = 100;//服务端版本号
        static public string WelcomeSentence = "欢迎加入本服务器，祝您玩的愉快！";//欢迎语句
        static public int PlayerMaxConnections = 3;//单个玩家的最大连接数
        static public bool IsClearEject = false, IsClearPlayer = true;//是否清除吐球/掉线玩家
        static public double EjectClearTime = 26;//吐球清除时间
        static public double PlayerClearTime = 56;//掉线玩家清除时间
        static public int Tick_Interval =60;//服务端处理周期

        //Map
        static public int BoarderWidth = 1200;//地图宽度
        static public int BoarderHeight = 1200;//地图高度
        static public double BoarderCover = 0.6;//细胞多少部分不与边界发生碰撞
        static public bool isBorderShrink = false;//地图是否缩小
        static public bool isOutBorderMassAtten = true;//地图外是否掉体积
        static public bool IsBoarderCollision = true;//是否边界碰撞
        static public bool IsBoarderCollisionBounce = true; //是否边界碰撞反弹

        //Food
        static public int AFood=6000;//食物数量
        static public int FoodMaxSize=20;//食物最大大小
        static public int FoodMinSize=12;//食物最小大小
        static public int FoodAppearsAmount = 20;//食物刷新数量
        static public float FoodAppearsCounter = 5;//食物刷新时间

        //Player
        static public int ViewWidth = 450;//视野初始宽度
        static public int ViewHeight = 450;//视野初始高度
        static public double PlayerStartSize = 10000;//玩家初始质量
        static public double CoverageDegree= 0.3;//覆盖系数
        static public double DevourSizeDegree=1.3;//吞噬系数
        static public double ReLiveWidth=80, ReLiveHeight=80;//重生范围
        static public double CollisionDeviationConstant = 0;//碰撞偏移指数
        static public double PlayerSizeRate = 0.99995;//玩家质量衰减速率

        static public double PlayerMoveSpeed = 1.26;//玩家移动速度
        static public double PlayerMoveSpeedRate = 0.247;//玩家移动速度衰减

        static public double PlayerMaxSize = 55500;//玩家自动分裂质量
        static public double PlayerLimitSize = 50000;//玩家极限质量

        static public double PlayerFusionTime = 13;//玩家融合时间
        static public double PlayerFussionStart = 0.1;//融合初始
        static public double PlayerFusionConstant = 0.12;//玩家融合系数
        static public double PlayerFusionDelay = 10;//玩家融合延迟

        static public int PlayerSplitLimit = 64;//玩家最大分裂数
        static public double PlayerSplitSpeed = 1.1;//玩家分裂速度
        static public double PlayerSplitSpeedRate = 0.98;//玩家分裂速度衰减
        static public double PlayerMinSplitSize = 69;//玩家最小分裂质量
        static public double PlayerCollisionTime = 16;//碰撞持续时间
        static public double PlayerCollisionConstant = 0.48;//碰撞计算系数
        static public int PlayerCollisionNumber = 1;//碰撞算法

        static public int PlayerEjectInterval = 0;//吐球间隔
        static public double PlayerMinEjectSize = 57;//最小吐球质量
        static public double PlayerEjectSize = 13;//吐球大小
        static public double PlayerEjectLose = 13;//吐球损失
        static public double PlayerEjectSpeed = 1.4;//吐球速度
        static public double PlayerEjectSpeedRate = 0.966;//吐球速度衰减
        static public bool EjectCollision = true;//吐球碰撞
        static public double PlayerViewZoom = 0.11;//玩家视野缩放
        static public double PlayerMassZoom = 0.11;//玩家质量缩放
        static public double SpectateWidth = 100;
        static public double SpectateHeight = 100;//观战视野大小

        //Virus
        static public int AVirus = 30;//病毒数量
        static public int MaxAVirus = 40;//病毒数量
        static public double VirusSize = 200;//病毒大小
        static public double VirusSplitSize = 400;//病毒分裂质量
        static public double VirusSplitSpeed = 1.7;//病毒分裂速度
        static public double VirusExplodeSpeed = 1.3;//病毒炸裂速度
        static public double VirusSplitSpeedRate = 0.95;//病毒分裂速度衰减
        static public bool IsVirusMoved = false;//病毒是否可移动
        static public bool PopSplit = true;

        //PlayerBot/Bot
        static public double BotMass=2500;
        static public int BotAmount=1;
        static public double MinionMass=2500;
        static public int MinionAmount = 10;
    }
}
