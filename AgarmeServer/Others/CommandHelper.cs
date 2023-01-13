using System;
using System.Text;

namespace AgarmeServer.Others
{
    /// <summary>
    /// 这个类的Listen函数可能会引发内存泄漏
    /// 运行时有频繁GC
    /// 先标记一下
    /// 等我后期再研究研究
    /// </summary>
    public class CommandHelper
    {
        public string Input { get; set; }
        public CommandHelper(string input)
        {
            Input = input;
        }
        public void Listen()
        {
            if (Program.server == null)
                return;
            var strArray = Input.ToLower().Split(" ");
            switch (strArray[0])
            {
                case "stop":
                    {
                        Program.world_manager.world.Stop();
                        BindMsg("正在尝试关闭服务器中");
                        Program.server.Stop();        
                        break;
                    }
                case "restart":
                    {
                        Program.world_manager.world.Stop();
                        BindMsg("正在尝试重启服务器中");
                        Thread.Sleep(1000);
                        if(Program.server.Restart())
                            BindMsg("重启服务器成功");
                        else
                            BindMsg("正在尝试重启服务器失败");
                        break;
                    }
                case "kick":
                    {
                        if (strArray.Length <= 1)
                        {
                            BindMsg("参数填写错误");
                            break;
                        }
                        int result;
                        if (!int.TryParse(strArray[1], out result))
                        {
                            BindMsg("参数填写错误");
                            break;
                        }
                        if (result == 0)
                        {
                            BindMsg("参数填写错误");
                            break;
                        }
                        var id = Convert.ToUInt32(strArray[1]);
                        if(Program.world_manager.PlayerOut(id))
                            BindMsg($"玩家{id}已被踢出服务器");
                        else
                            BindMsg($"玩家{id}不在所在服务器世界列表中");
                        break;
                    }
                case "cn":
                    {
                        if (strArray.Length <= 1)
                        {
                            BindMsg("参数填写错误");
                            break;
                        }
                        int result;
                        if (!int.TryParse(strArray[1], out result))
                        {
                            BindMsg("参数填写错误");
                            break;
                        }
                        ServerConfig.PlayerCollisionNumber = result;

                        break;
                    }
                case "playerlist":
                    {
                        if (strArray.Length > 1)
                        {
                            BindMsg("该命令没有参数");
                            break;
                        }
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("---------------------------玩家列表--------------------------- ");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        foreach (var val in Program.world_manager.world.PlayerList.Values)
                        {
                            if(val.BT is not 0) Console.WriteLine($"ID:{val.BT}\t名称:{val.Name}");                          
                        }
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("-------------------------------------------------------------- ");

                        break;
                    }
                case "help":
                    {
                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                        Console.WriteLine("\n\t\t\tAgarme Help Command\n\tStop:停止服务器\n\tMass (PlayerId) (mass):赋予指定ID的细胞质量\n\tConsole (string):调试输出一个指定的文本\n\t" +
                       "Stop:关闭服务器\n\tRestart:重启服务器\n\tClear:清除控制台内容\n\tReload:重新载入配置\n\tShrink:地图缩小\n\tBCo:地图边界碰撞\n\tBCB:边界碰撞反弹\n\tOt:地图外质量减小\n\tEc:吐球碰撞\n\tPd:玩家掉线删除\n\tcn (number):使用指定编号的碰撞算法\n\tPlayerlist:玩家id列表\n\tKill (id):使指定id的细胞死亡\n\tVirus (id):在指定id的所有玩家细胞上生成病毒\n\tMove (id) (x) （y）;移动指定玩家到指定坐标\n\tFussion (id)使指定玩家融合\n\tName (id) (Name):修改指定玩家的名称\n\tInform (contents):在客户端聊天框发送一个系统消息\n\tRestart:重启或清空地图并重载\n\tplayer (id):查看玩家信息\n\tkick (id):踢出玩家\n\t(指令不区分大小写)");
                        break;
                    }
                case "clear":
                    {
                        Console.Clear();
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("---------------------欢迎使用Agarme服务端--------------------- ");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"\t\t版本号:{ServerConfig.version}\t作者:Afkeru");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("系统消息：服务器启动成功");
                        Console.WriteLine("系统消息：游戏监听已成功启动，键入‘help'可查看帮助指令");

                        break;
                    }
                default:
                    {
                        BindMsg($"'{Input}'是一个无效的命令,请检查命令是否正确!");
                        break;
                    }
            }
        }
        private void BindMsg(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("系统消息："+msg);
        }
    }
}
