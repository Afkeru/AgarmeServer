using System;
using System.Runtime.CompilerServices;
using AgarmeServer.Client;
using AgarmeServer.Entity;
using AgarmeServer.Zeroer;
using HPSocket.Base;
using HPSocket.Tcp;
namespace AgarmeServer.Network
{
    public class GameServer : TcpPackServer,IDisposable
    {
        public bool IsRestart = false;
        private Session session;
        public GameServer():base()
        {
            this.PackHeaderFlag = 0;
            this.MaxPackSize = 0x3FFFFF;
            session = new Session(this);
        }
        public bool ConnectAndStart(string ip, ushort port)
        {
            try
            {
                this.Address = ip.Trim();
                this.Port = port;
                return this.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("系统消息：服务端捕捉到一个连接错误，错误原因为："+ex.Message);
                return false;
            }
        }
        public bool Restart()
        {
            try
            {
                IsRestart = true;
                this.Stop();
                PlayerClient.MaxBT = 1;
                Cell.IdGenerator = new IdPool();
                Program.world_manager.ClearWorld();
                IsRestart = false;
                Program.world_manager.world.Start();
                return this.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("系统消息：服务端捕捉到一个连接错误，错误原因为：" + ex.Message);
                return false;
            }
        }
    }
}
