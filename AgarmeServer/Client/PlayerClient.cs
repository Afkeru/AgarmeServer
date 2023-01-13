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
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;

namespace AgarmeServer.Client
{
    public class PlayerClient:IClient
    {
        public static uint MaxBT = 1;
        public MinionClient MyMinion = null;

        public Queue<byte> SplitQueue= new Queue<byte>();

        public ClientState State { set; get; }//玩家当前状态
        public HKPoint Mouce { set; get; }//实际鼠标坐标
        public uint BT { set; get; }//玩家的所属
        public IntPtr Connid { set; get; }//玩家的Connid
        public bool Split { set; get; }//判断是否按下分裂
        public bool Eject { set; get; }//判断是否按下吐球
        public bool Tab { set; get; }//判断是否按下Tab键
        public bool Macro { set; get; }
        public bool PlayerBotSplit { set; get; }//判断是否按下人机分裂
        public bool PlayerBotEject { set; get; }//判断是否按下人机吐球

        public PlayerClient(IntPtr _ConnId,World _world)
        {
            State = ClientState.Connected;

            Connid = _ConnId;

            Focus = true;

            var _x = ServerConfig.BoarderWidth * 0.5d;
            var _y = ServerConfig.BoarderHeight * 0.5d;

            LastMouceLoc = Mouce = new HKPoint(_x, _y);

            AverageViewX = HKRand.Double(0, ServerConfig.BoarderWidth);
            AverageViewY = HKRand.Double(0, ServerConfig.BoarderHeight);
        }

        //客户端Tick
        public unsafe void Tick(World world)
        {
            var Cells_Length = 0u;
            var wb = new WritableBuffer();

            SetView();
            SetSightWH(MyMinion);

            DeadMonitor();

            HandleDual(world);

            for (var ii = 0; ii < world.Cells.Count; ii++)
            {
                Cell cell = world.Cells[ii];

                if (cell.Deleted)
                {
                    switch (cell.Type)
                    {
                        case Constant.TYPE_FOOD:
                            {
                                var id_bytes = new byte[5];
                                id_bytes[0] = 146;
                                BufferWriter.WriteUnmanaged(cell.Id, id_bytes, 1);
                                Send(id_bytes);
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
                }

                #region Serialize
                if (cell.Type is Constant.TYPE_FOOD)
                {
                    var food = cell as Food;
                    food.Serialize(wb, this, ref Cells_Length);
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
                if (cell.Type is Constant.TYPE_BOT)
                {
                    var minion = cell as Minion;
                    minion.Serialize(wb, this,world, ref Cells_Length);
                }
                #endregion
            }

            for (var ii = 0; ii < world.PlayerCells.Count; ii++)
            {
                var player = world.PlayerCells[ii] as Player;
                player.Serialize(wb, this, world, ref Cells_Length);
            }

            Send(BufferWriter.WritePackage(143, wb.Buffer.ToArray(), Cells_Length));

            //发送数据
            if (State is ClientState.Playing)
            {
                byte[] bytes = new byte[10];
                bytes[0] = 147;
                BufferWriter.WriteUnmanaged((ushort)ViewArea.Width, bytes, 1);
                BufferWriter.WriteUnmanaged((ushort)ViewArea.Height, bytes, 3);
                BufferWriter.WriteUnmanaged((float)(Mass+MyMinion.Mass), bytes, 5);
                byte _tab = Tab ? (byte)1 : (byte)0;
                BufferWriter.WriteUnmanaged(_tab, bytes, 9);
                Send(bytes);
            }

            Cells_Length = 0;

            #region 处理空格
            if (Focus)
            {
                var len = world.PlayerCells.Count;
                for (int i = 0; i < len; ++i)
                {
                    var player = world.PlayerCells[i] as Player;
                    if (player.BT == BT)
                    {
                        if (SplitAttempts > 0)
                        {
                            SpaceCount++;
                            player.Split(Mouce, ServerConfig.PlayerSplitSpeed * Math.Pow(player.Size, -0.001), world);
                        }
                    }
                }
                if (SplitAttempts > 0) SplitAttempts--;
            }

            if (MyMinion.Focus)
            {
                for (int i = 0; i < world.Cells.Count; i++)
                {
                    if (world.Cells[i].Type is Constant.TYPE_BOT)
                    {
                        var minion = world.Cells[i] as Minion;
                        if (minion.Number == BT)
                        {
                            if (SplitAttempts > 0)
                            {
                                MyMinion.SpaceCount++;
                                minion.Split(Mouce, ServerConfig.PlayerSplitSpeed * Math.Pow(minion.Size, -0.001), world);
                            }
                        }
                    }
                }
                if (SplitAttempts > 0) SplitAttempts--;
            }
            #endregion

            world.BtList.Clear();

            Reset();
            wb.Clear();
            wb.Buffer.Free();
            wb = null;
        }

        public void Reset()
        {
            PlayerBotEject = false;
            PlayerBotSplit = false;
            ViewX = 0;
            ViewY = 0;
            Mass = 0;
            LastSplitCount = SplitCount;
            SplitCount = 0;
            SpaceCount = 0;
            Eject = false;
            Split = false;
        }
        public void DeadMonitor()
        {
            if (State is ClientState.Playing)
            {
                if (OwnCells.Count is <= 0 && Mass <= 0 && MyMinion.OwnCells.Count is <= 0 && MyMinion.Mass <= 0)
                {
                    State = ClientState.Connected;
                    Tab = false;
                    Send(new byte[] { 148 });
                }
            }
        }
        public void HandleDual(World world)
        {
            #region Minion Data
            if (MyMinion is null)
            {
                MyMinion = new MinionClient() { Number = BT };
                MyMinion.Parent = this;
                MyMinion.Name = "侍从";
            }
            else
            {
                if (Tab)
                {
                    MyMinion.Focus = true;
                    Focus = false;
                }
                else
                {
                    MyMinion.Focus = false;
                    Focus = true;
                }

                if (MyMinion.Focus)
                {
                    if (MyMinion.OwnCells.Count is <= 0)
                    {
                        CellCreator.Generate_Minion(MyMinion, world);
                        MyMinion.Die = false;
                    }
                }

                if (State is ClientState.Playing)
                {
                    if (Focus)
                    {
                        if (OwnCells.Count is <= 0 && Mass <= 0)
                        {
                            if (MyMinion.OwnCells.Count is > 0) CellCreator.Generate_Player_Around_Minion(this, world);
                            else CellCreator.Generate_Player(this, world);
                            MyMinion.Die = false;
                        }
                    }
                }

            }
            MyMinion.ViewX = 0;
            MyMinion.ViewY = 0;
            MyMinion.Mass = 0;
            MyMinion.LastSplitCount = SplitCount;
            MyMinion.SplitCount = 0;
            MyMinion.SpaceCount = 0;
            #endregion
        }
        public void SetView()
        {
            if (State is ClientState.Playing)
            {
                if (SplitCount is not 0)
                {
                    AverageViewX = ViewX / SplitCount;
                    AverageViewY = ViewY / SplitCount;
                }
            }
        }
        public void AllocateBT() { this.BT = PlayerClient.MaxBT; PlayerClient.MaxBT++; }
        public bool Send(List<byte> val) => Program.server.Send(Connid, val.ToArray(), val.Count);
        public bool Send(byte[] val) => Program.server.Send(Connid, val.ToArray(), val.Length);
    }
}
