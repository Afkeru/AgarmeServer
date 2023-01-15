using AgarmeServer;
using AgarmeServer.Entity;
using AgarmeServer.HKObj;
using AgarmeServer.Others;
using AgarmeServer.Map;
using AgarmeServer.Zeroer;
using AgarmeServer.Network;
using System.Numerics;

namespace AgarmeServer.Client
{
    public class PlayerClient:IClient
    {
        public static uint MaxBT = 1;
        public MinionClient MyMinion = null;

        public ClientState State { set; get; }//玩家当前状态
        public HKPoint Mouce { set; get; } = new();//实际鼠标坐标
        public uint BT { set; get; }//玩家的所属
        public IntPtr Connid { set; get; }//玩家的Connid
        public bool Split { set; get; }//判断是否按下分裂
        public bool Eject { set; get; }//判断是否按下吐球
        public bool Tab { set; get; }//判断是否按下Tab键
        public bool Macro { set; get; }

        public PlayerClient(IntPtr _ConnId,World _world)
        {
            State = ClientState.Connected;

            Connid = _ConnId;

            Focus = true;

            var _x = ServerConfig.BoarderWidth * 0.5d;
            var _y = ServerConfig.BoarderHeight * 0.5d;

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

            UpdateClientState();

            foreach (var cell in world.Cells)
            {
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
                    virus.Serialize(wb, this, ref Cells_Length);
                }
                if (cell.Type is Constant.TYPE_EJECT)
                {
                    var virus = cell as Eject;
                    virus.Serialize(wb, this, ref Cells_Length);
                }
                if (cell.Type is Constant.TYPE_BOT)
                {
                    var minion = cell as Minion;
                    minion.Serialize(wb, this, world, ref Cells_Length);
                }
                #endregion
            }

            for (var i = (world.PlayerCells.Count - 1); i > -1; i--)
                world.PlayerCells[i].Serialize(wb, this, world, ref Cells_Length);

            for (var i = (world.BotCells.Count - 1); i > -1; i--)
                world.BotCells[i].Serialize(wb, this, world, ref Cells_Length);

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

            #region 处理空格
            //处理玩家的细胞空格
            var my_cells = OwnCells.Values.ToArray();
            var my_cells_len = my_cells.Length;
            if (SplitAttempts is > 0)
            {
                for (int i = 0, l = OwnCells.Values.Count; i < l; ++i)
                {
                    if (my_cells_len >= ServerConfig.PlayerSplitLimit)
                        break;
                    var player = my_cells[i] as Player;
                    player.Split_Edited(Mouce, ServerConfig.PlayerSplitSpeed * Math.Pow(player.Size, -0.001), world);
                    SpaceCount++;
                }
                SplitAttempts--;
            }

            //处理侍从的细胞空格
            if(MyMinion is not null)
            {
                var my_minion_cells = MyMinion.OwnCells.Values.ToArray();
                var my_minion_cells_len = my_minion_cells.Length;
                for (int i = 0, l = my_minion_cells_len; i < l; ++i)
                {
                    if (my_minion_cells_len >= ServerConfig.PlayerSplitLimit)
                        break;
                    var minion = my_minion_cells[i] as Minion;
                    if (MyMinion.SplitAttempts is > 0)
                    {
                        MyMinion.SpaceCount++;
                        minion.Split_Edited(Mouce, ServerConfig.PlayerSplitSpeed * Math.Pow(minion.Size, -0.001), world);
                    }
                }
                if (MyMinion.SplitAttempts is > 0) MyMinion.SplitAttempts--;
            }
            #endregion

            Reset();
            wb.Clear();
            world.BtList.Clear();
            wb.Buffer.Free();
            Cells_Length = 0;
            wb = null;
        }

        public void OnDead() {Die = true; Tab=true; Focus = false; Mass = 0; SpaceCount = 0; SplitCount = 0; ViewX = 0; ViewY = 0; ViewArea = new HKRectD(0, 0, 0, 0); }
        public void OnClose() { State = ClientState.Connected; Mass = 0; SpaceCount = 0; SplitCount = 0; ViewX = 0; ViewY = 0; ViewArea = new HKRectD(0, 0, 0, 0); }
        public void UpdateClientState()
        {
            if (State is not ClientState.Playing) return;
            var player_die = OwnCells.Count <= 0;
            var bot_die = MyMinion.OwnCells.Count <= 0;
            if (player_die && !bot_die)
            {
                Tab = true;
                Die = true;
            }

            if (bot_die && !player_die)
            {
                Tab = false;
                MyMinion.Die = true;
            }

            //if(player_die && bot_die)
            //{
            //    State = ClientState.Connected;
            //    Die = true;
            //    MyMinion.Die = true;
            //}
        }
        public void Reset()
        {
            ViewX = 0;
            ViewY = 0;
            Mass = 0;
            LastSplitCount = SplitCount;
            SplitCount = 0;
            SpaceCount = 0;
            CurrentBiggestSize = 0;
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
                MyMinion = new MinionClient() { Number = BT, Parent = this, Name = this.Name + "的人机" };
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

                if (State is ClientState.Playing)
                {
                    if (Tab is false)
                    {
                        if (OwnCells.Count is <= 0 || Mass <= 0)
                        {
                            if (MyMinion.OwnCells.Count is > 0) CellCreator.Generate_Player_Around_Minion(this, world);
                            else CellCreator.Generate_Player(this, world);
                            MyMinion.Die = false;
                            Die = false;
                        }
                    }
                    else
                    {
                        if (MyMinion.OwnCells.Count is <= 0 || MyMinion.Mass <= 0)
                        {
                            CellCreator.Generate_Minion(MyMinion, world);
                            MyMinion.Die = false;
                            Die = false;
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
