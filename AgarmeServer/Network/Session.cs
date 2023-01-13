using System;
using System.Numerics;
using System.Text;
using AgarmeServer.Client;
using AgarmeServer.Entity;
using AgarmeServer.HKObj;
using AgarmeServer.Map;
using HPSocket;
namespace AgarmeServer.Network
{
    public class Session
    {
        private GameServer server;
        private MatchManager manager;
        public Session(GameServer arg)
        {
            server = arg;
            manager = Program.world_manager;
            server.OnPrepareListen += new ServerPrepareListenEventHandler(OnPrepareListen);
            server.OnAccept += new ServerAcceptEventHandler(OnAccept);
            server.OnReceive += new ServerReceiveEventHandler(OnReceive);
            server.OnClose += new ServerCloseEventHandler(OnClose);
            server.OnShutdown += new ServerShutdownEventHandler(OnShutDown);
        }
        unsafe private HandleResult OnReceive(IServer sender, IntPtr connId, byte[] data)
        {
            var key = (uint)connId;

            var offset = 1;

            var client = manager.world.PlayerList[key];

            fixed (byte* p = data)
            {
                switch (data[0])
                {
                    case 136:
                        {
                            client.Mouce.X = BufferReader.Read<float>(p, ref offset);
                            client.Mouce.Y = BufferReader.Read<float>(p, ref offset);
                            if (data[offset++] is 1) client.Eject = true;
                            if (data[offset++] is 1) client.PlayerBotSplit = true;
                            if (data[offset++] is 1) client.PlayerBotEject = true;
                            break;
                        }
                    case 138:
                        {
                            int len = BufferReader.Read<ushort>(p, ref offset);

                            var receive =BufferReader.ReadStr(data, len, ref offset);
                            
                            if (receive.ToLower().Trim() is "macro") //macro
                            {
                                client.Macro = !client.Macro;
                                receive = client.Macro ? "Macro enabled successfully" : "Macro disable successfully";
                            }

                            var msg = client.Name + ":" + receive;

                            client.Send(BufferWriter.WritePackage(139, BufferWriter.WriteString(msg)));

                            break;
                        }
                    case 149:
                        {
                            client.Tab = !client.Tab;
                            if(client.Tab) client.LastMouceLoc = client.Mouce;
                            else if(client.MyMinion is not null)
                                client.MyMinion.LastMouceLoc = client.Mouce;
                            break;
                        }
                    case 166:
                        {
                            var length = BufferReader.Read<ushort>(p, ref offset);
                            client.Name = BufferReader.ReadStr(data, length, ref offset);
                            /*玩家处于观战状态*/
                            if (client.State is ClientState.Connected)
                            {
                                client.State = ClientState.Playing;
                                client.Die = false;
                                client.Send(new byte[] { 144 });
                                CellCreator.Generate_Player(client, manager.world);
                            }
                            break;
                        }
                    case 150:
                        {
                            client.SplitQueue.Enqueue(1);
                            client.SplitAttempts++;
                            break;
                        }
                }
            }


            return HandleResult.Ok;
        }
        private HandleResult OnClose(IServer sender, IntPtr connId, SocketOperation socketOperation, int errorCode)
        {
            Console.WriteLine($"ID为：{connId}的玩家退出了游戏");
            manager.PlayerOut((uint)connId);
            return HandleResult.Ok;
        }
        private HandleResult OnAccept(IServer sender, IntPtr connId, IntPtr client)
        {
            Console.WriteLine($"ID为：{connId}的玩家加入了服务器");

            PlayerClient temp = new PlayerClient(connId, manager.world) { BT = (uint)connId};

            //if()
            //{
            //    string msg =  "系统消息:该服务器已满，请过段时间再重试" ;

            //    temp.Send(BufferWriter.WritePackage(139, BufferWriter.WriteString(msg)));

            //    Program.server.Disconnect(connId);
            //}

            byte[] bytes = new byte[9];
            bytes[0] = 137;
            BufferWriter.WriteUnmanaged((ushort)(temp.BT), bytes, 1);
            BufferWriter.WriteUnmanaged((ushort)ServerConfig.BoarderWidth, bytes, 3);
            BufferWriter.WriteUnmanaged((ushort)ServerConfig.BoarderHeight, bytes, 5);
            BufferWriter.WriteUnmanaged(ServerConfig.version_code, bytes, 7);
            Program.server.Send(connId, bytes, bytes.Length);

            manager.PlayerIn(temp);

            return HandleResult.Ok;
        }
        private HandleResult OnPrepareListen(IServer sender, IntPtr listen)=>HandleResult.Ok;
        private HandleResult OnShutDown(IServer sender)=>HandleResult.Ok;
    }
}
