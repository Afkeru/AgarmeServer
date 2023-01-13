using NetCoreServer;
using AgarmeServer.Network;
using AgarmeServer.Others;
using AgarmeServer.Map;
using System.Runtime.InteropServices;

namespace AgarmeServer
{
    internal class Program
    {
        #region DLL导入
        const int STD_INPUT_HANDLE = -10;
        const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
        const uint ENABLE_INSERT_MODE = 0x0020;
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int hConsoleHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint mode);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint mode);
        #endregion
        public static GameServer server;
        private static Thread ListenThread;
        private static CommandHelper command = new CommandHelper("");
        public static MatchManager world_manager = new MatchManager();
        static void Main(string[] args)
        {
            //对控制台窗口进行初始化设置
            IniConsole();

            //打印初始化信息
            PrintFrontConsole();

            //开始监听
            world_manager.world.Start();

            ListenThread = new Thread(ListenFunc);
            ListenThread.Start();
            ListenThread.Join();

            //打印服务器断开信息
            PrintBreakConsole();
        }
        private static void ListenFunc(object obj)
        {
            if (server == null)
                return;
            while (server.HasStarted && !server.IsRestart)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                //控制台命令监听
                command.Input = Console.ReadLine();
                command.Listen();
            }
        }
        private static void IniConsole()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WindowWidth = 62;
                Console.WindowHeight =33;
                Console.BufferHeight = 150;
            }
            Console.Title = "Agarme终端" + ServerConfig.version;
            IntPtr hStdin = GetStdHandle(STD_INPUT_HANDLE);
            uint mode;
            GetConsoleMode(hStdin, out mode);
            mode &= ~ENABLE_QUICK_EDIT_MODE;  //移除快速编辑模式
            mode &= ~ENABLE_INSERT_MODE;      //移除插入模式
            SetConsoleMode(hStdin, mode);
        }
        private static void PrintFrontConsole()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("---------------------欢迎使用Agarme服务端--------------------- ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"\t\t版本号:{ServerConfig.version}\t作者:Afkeru");
            Console.ForegroundColor = ConsoleColor.Red;
            server = new GameServer();
            Console.Write("系统消息：服务器正在启动中");
            for(var i = 0; i < 5; ++i)
            {
                Console.Write(".");
                Thread.Sleep(300);
            }
            if (server.ConnectAndStart(ServerConfig.ip, ServerConfig.port))
            {
                Console.Write("服务器启动成功");
                Thread.Sleep(620);
                Console.WriteLine();
                Thread.Sleep(650);
                Console.WriteLine("Ip地址："+ServerConfig.ip);
                Thread.Sleep(450);
                Console.WriteLine("端口号：" + ServerConfig.port.ToString());
                Thread.Sleep(500);
                Console.WriteLine("系统消息：游戏监听已成功启动，键入‘help'可查看帮助指令");
            }
            else
                Console.Write("系统消息：服务器启动失败，请根据错误提示进行检查再重试");
        }
        private static void PrintBreakConsole()
        {
            Thread.Sleep(900);
            Console.WriteLine();
            Console.WriteLine("系统消息：游戏服务器已停止运行，按任意键可退出该窗口");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("--------------------------------------------------------------");
            //按任意键关闭窗口
            Console.ReadKey();
        }
    }
}