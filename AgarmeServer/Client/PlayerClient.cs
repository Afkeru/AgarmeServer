using AgarmeServer;
using AgarmeServer.Entity;
using AgarmeServer.HKObj;
using AgarmeServer.Others;
using AgarmeServer.Map;
using AgarmeServer.Zeroer;
using HPSocket.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using NetCoreServer;
using AgarmeServer.Network;
using HPSocket;
using System.Numerics;
using System.Collections;
using System.Text;

namespace AgarmeServer.Client
{
    public class PlayerClient:IClient
    {
        public static uint MaxBT = 1;
        public ClientState State { set; get; }//玩家当前状态
        public HKPoint Mouce { set; get; }//实际鼠标坐标
        public HKPoint MouseReceive { set; get; }//接收到的鼠标坐标
        public bool Split { set; get; }//判断是否按下分裂
        public bool Eject { set; get; }//判断是否按下吐球
        public bool PlayerBotSplit { set; get; }//判断是否按下人机分裂
        public bool PlayerBotEject { set; get; }//判断是否按下人机吐球

        public PlayerClient(IntPtr _ConnId,World _world)
        {
            State = ClientState.Connected;
            Connid = _ConnId;

            var _x = ServerConfig.BoarderWidth * 0.5d;
            var _y = ServerConfig.BoarderHeight * 0.5d;

            Mouce = new HKPoint(_x, _y);
            MouseReceive = new HKPoint(_x, _y);
            AverageViewX = HKRand.Double(0, ServerConfig.BoarderWidth);
            AverageViewY = HKRand.Double(0, ServerConfig.BoarderHeight);
        }

        //客户端Tick
        public unsafe void Tick(World world)
        {
            uint Cells_Length = 0;
            WritableBuffer wb = new WritableBuffer();

            this.SetView();
            this.SetSightWH();

            //if (this.State is ClientState.Playing)
            //{
            //    if (CheckAlive())
            //    {
            //        this.DeathLoc = new HKPoint(this.AverageViewX, this.AverageViewY);
            //        this.State = ClientState.Connected;
            //    }
            //}

            if(Deleted) return;


            for (var ii = 0; ii < world.CellList.Count; ii++)
            {
                Cell cell = world.CellList[ii];
                if (cell is null) continue;
                if (cell.Deleted)
                {
                    switch (cell.Type)
                    {
                        case Constant.TYPE_FOOD:
                            {
                                var id_bytes = new byte[5];
                                id_bytes[0] = 146;
                                BufferWriter.WriteUnmanaged(cell.Id, id_bytes, 1);
                                if (cell.EatenBy.Type is Constant.TYPE_PLAYER)
                                {
                                    var player = cell.EatenBy as Player;
                                    player.Client.Send(id_bytes);
                                }
                                break;
                            }
                        case Constant.TYPE_VIRUS:
                            {
                                var virus = cell as Virus;
 
                                var bytes = new byte[5];
                                bytes[0] = 146;
                                BufferWriter.WriteUnmanaged(virus.Id, bytes, 1);
                                Send(bytes);
                                break;

                            }
                        case Constant.TYPE_EJECT:
                            {
                                var eject = cell as Eject;
                                var bytes = new byte[5];
                                bytes[0] = 146;
                                BufferWriter.WriteUnmanaged(eject.Id, bytes, 1);
                                Send(bytes);
                                break;
                            }
                    }
                    continue;
                }

                if (cell.Type is Constant.TYPE_FOOD)
                {
                    var food = cell as Food;
                    food.Serialize(wb, this, ref Cells_Length);
                }
                if (cell.Type is Constant.TYPE_PLAYER)
                {
                    var player = cell as Player;
                    player.Serialize(wb, this, world, ref Cells_Length);
                }
                if (cell.Type is Constant.TYPE_VIRUS)
                {
                    var virus = cell as Virus;
                    virus.Serialize(wb,this,ref Cells_Length);
                }
                if (cell.Type is Constant.TYPE_EJECT)
                {
                    var virus = cell as Eject;
                    virus.Serialize(wb,this,ref Cells_Length);
                }
            }


            Send(BufferWriter.WritePackage(143, wb.Buffer.ToArray(), Cells_Length));

            //发送视野数据
            if (State is ClientState.Playing)
            {
                byte[] bytes = new byte[9];
                bytes[0] = 147;
                BufferWriter.WriteUnmanaged((ushort)ViewArea.Width, bytes, 1);
                BufferWriter.WriteUnmanaged((ushort)ViewArea.Height, bytes, 3);
                BufferWriter.WriteUnmanaged((float)Mass, bytes, 5);
                Send(bytes);
            }

            Cells_Length = 0;
            PlayerBotEject = false;
            PlayerBotSplit = false;
            LastSplitCount = SplitCount;
            ViewX = 0;
            ViewY= 0;
            Mass = 0;
            SplitCount = 0;
            SpaceCount = 0;
            Eject = false;
            Split = false;

            world.BtList.Clear(); 
            wb.Clear();
            wb.Buffer.Free();
            wb = null;

        }

        public void SetView()
        {
            if (State is ClientState.Playing)
            {
                if (SplitCount is not 0)
                {
                    AverageViewX = ViewX / SplitCount;
                    AverageViewY = ViewY / SplitCount;
                    //Console.WriteLine($"{ViewX},{ViewY},{SplitCount},{Mass}");
                }
            }
            else
            {
                var temp_cell = new Cell() { X = AverageViewX, Y = AverageViewY, Size = 3300 };
                temp_cell.MoveTo(Mouce, 2.1, ref AverageViewX, ref AverageViewY);
                if (AverageViewX < 0)
                    AverageViewX = 0;
                if (AverageViewX > ServerConfig.BoarderWidth)
                    AverageViewX = ServerConfig.BoarderWidth;
                if (AverageViewY < 0)
                    AverageViewY = 0;
                if (AverageViewY > ServerConfig.BoarderHeight)
                    AverageViewY = ServerConfig.BoarderHeight;
            }
        }

        public void AllocateBT() { this.BT = PlayerClient.MaxBT; PlayerClient.MaxBT++; }
    }
}
