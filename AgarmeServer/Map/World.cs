using AgarmeServer.Client;
using AgarmeServer.Entity;
using AgarmeServer.Network;
using AgarmeServer.Others;
using AgarmeServer.Map;
using AgarmeServer.Zeroer;
using System;
using System.Timers;
using System.Diagnostics;
using HPSocket.Base;
using System.Numerics;
using System.Drawing;
#pragma warning disable 219, 414

namespace AgarmeServer.Map
{
    public class World
    {
        public static uint MaxHandle = 1;
        public UsingLock<object> Cells_Lock;
        public UsingLock<object> Client_Lock;
        public readonly uint Handle=0;
        public uint PresentPlayer = 0;

        public DateTimeOffset startTime;
        public double avargateTickTime;
        public int tick;
        public int tickDelay;
        public float stepMult;
        public long ServerTimeInMilliseconds => ticker.ServerTimeInMilliseconds;
        public uint FoodAmount = 0;
        public uint VirusAmount = 0;
        public bool running = false;
        private float FoodTick = 0;

        public List<Cell> BoostCell = new();
        public List<uint> BtList = new();
        public List<Cell> Cells = new();
        public List<Player> PlayerCells = new();
        public List<Minion> BotCells = new();
        public QuadTree<Cell> quadtree = new QuadTree<Cell>(new System.Drawing.RectangleF(0,0,ServerConfig.BoarderWidth,ServerConfig.BoarderHeight),32, 16, null);
        public Dictionary<uint,PlayerClient> PlayerList = new();
        public Ticker ticker = new Ticker(40);
        private Stopwatch stopWatch = new();
        private Queue<Cell> DeleteList = new();


        public World()
        {
            Handle = MaxHandle++;

            Cells_Lock= new UsingLock<object>();
            Client_Lock= new UsingLock<object>();

            tickDelay = (1000 / ServerConfig.Tick_Interval);
            ticker.step = tickDelay;
            stepMult = tickDelay / 40;
            ticker.Add(Tick);
        }

        public bool Start()
        {
            if (running) return false;
            startTime = DateTimeOffset.Now;
            avargateTickTime = tick = 0;
            running = true;
            ticker.Start();
            return true;
        }

        public bool Stop()
        {
            if (!running) return false;
            ticker.Stop();
            avargateTickTime = tick = 0;
            running = false;
            return true;
        }

        public unsafe void Tick()
        {
            stopWatch.Restart();
            tick++;

            if (Program.server.HasStarted is not true) return;

            if (ServerConfig.isBorderShrink) { ServerConfig.BoarderWidth--; ServerConfig.BoarderHeight--; }

            var r1 = 0.0d;

            var r2 = 0.0d;

            BoostCells();
            //遍历其他细胞
            Parallel.For(0, Cells.Count, (i, state) =>
            {
                var cell = Cells[i];
                r1 = cell.R;

                cell.MonitorBorderCollide();

                #region 细胞逻辑处理
                //食物
                if (cell.Type is Constant.TYPE_FOOD)
                {
                    var food = cell as Food;
                    FoodAmount++;
                    food.Tick();
                }

                //病毒
                if (cell.Type is Constant.TYPE_VIRUS)
                {
                    var virus = cell as Virus;
                    VirusAmount++;
                    virus.Tick(this);
                }

                //吐球
                if (cell.Type is Constant.TYPE_EJECT)
                {
                    var eject = cell as Eject;
                    eject.Tick(this);
                }

                //侍从
                if (cell.Type is Constant.TYPE_BOT)
                {
                    var minion = cell as Minion;
                    minion.Tick(this);
                }

                //人机
                if (cell.Type is Constant.TYPE_ROBOT)
                {
                }
                #endregion
            });

            //遍历玩家的人机
            var bot_cells = BotCells.ToArray();
            if (ServerConfig.IsSolotrick)
                Array.Sort(bot_cells);
            for(var i= bot_cells.Length-1; i >= 0; --i)
            {
                var minion = bot_cells[i];

                if (minion.Client.Parent.Deleted)
                {
                    if (ServerConfig.IsClearPlayer)
                    {
                        if (minion.Deleted_Clear_Tick < ServerConfig.PlayerClearTime)
                            minion.Deleted_Clear_Tick++;
                        else
                        {
                            minion.Name = "Dead";
                            minion.Deleted = true;
                            minion.color_index = 112;
                            minion.Transverse = 0;
                            minion.Longitudinal = 0;
                            minion.Deleted_Clear_Tick = 0;
                        }
                    }
                }
                minion.MonitorBorderCollide();

                minion.Tick(this);
            }

            //处理玩家和客户组
            HandlePlayer();

            DeleteCells();

            UpdateFoodAndVirus();

            avargateTickTime = stopWatch.Elapsed.TotalMilliseconds;
            stopWatch.Reset();
        }
        #region 辅助函数
        private void UpdateFoodAndVirus()
        {
            ////刷新病毒
            while(VirusAmount < ServerConfig.AVirus)
            {
                CellCreator.Generate_Virus(1, this);
                VirusAmount++;
            }

            //刷新食物
            while(FoodAmount < ServerConfig.AFood)
            {
                if (FoodTick >= ServerConfig.FoodAppearsCounter)
                {
                    CellCreator.Generate_Food(ServerConfig.FoodAppearsAmount, this);
                    FoodTick = 0;
                    FoodAmount++;
                    break;
                }
                else FoodTick++;
            }

            VirusAmount = 0;
            FoodAmount  = 0;

        }

        private void BoostCells()
        {
            for (int i = 0; i < BoostCell.Count; ++i)
            {
                var cell = BoostCell[i];
                switch (cell.Type)
                {
                    case Constant.TYPE_PLAYER:
                        {
                            PlayerCells.Add(cell as Player);
                            break;
                        }
                    case Constant.TYPE_BOT:
                        {
                            BotCells.Add(cell as Minion);
                            break;
                        }
                    default:
                        {
                            Cells.Add(cell);
                            break;
                        }
                }
                quadtree.Insert(cell);
            }
            BoostCell.Clear();
        }

        private void DeleteCells()
        {
            for (int i = Cells.Count-1; i >=0; i--)
            {
                Cell cell = Cells[i];

                if (cell.Deleted)
                {
                    cell.ReturnID();
                    cell.Id = 0;
                    Cells.Remove(cell);
                    quadtree.Remove(cell);
                    if(cell.Type==Constant.TYPE_FOOD)
                        ((Food)cell).Dispose();
                    if (cell.Type == Constant.TYPE_EJECT)
                        ((Eject)cell).Dispose();
                    if (cell.Type == Constant.TYPE_VIRUS)
                        ((Virus)cell).Dispose();
                }

            }

            for (int i = PlayerCells.Count - 1; i>=0; i--)
            {
                var player = PlayerCells[i];

                if (player is null) continue;


                if (player.Deleted)
                {
                    player.ReturnID();
                    player.Client.OwnCells.Remove(player.Id);
                    player.Id = 0;
                    PlayerCells.RemoveAt(i);
                    quadtree.Remove(player);
                    player.Dispose();
                    //player = null;
                }
            }

            for (int i = BotCells.Count - 1; i >= 0; i--)
            {
                var minion = BotCells[i];

                if (minion is null) continue;

                if (minion.Deleted)
                {
                    minion.ReturnID();
                    minion.Client.OwnCells.Remove(minion.Id);
                    minion.Id = 0;
                    BotCells.RemoveAt(i);
                    quadtree.Remove(minion);
                    minion.Dispose();
                    //minion = null;
                }
            }
        }

        private void HandlePlayer()
        {
            var keys = PlayerList.Keys.ToArray();
            for (int i = 0, l = keys.Length; i < l; i++)
            {
                PlayerClient client = PlayerList[keys[i]];

                if (client is null) { PlayerList.Remove(client.BT);continue; }
                if (client.Deleted) { PlayerList.Remove(client.BT);continue; }

                var player_cells = client.OwnCells.Values.ToArray();
                if(ServerConfig.IsSolotrick) 
                    Array.Sort(player_cells);
                for (int j= player_cells.Length-1; j>=0;--j)
                {
                    var player = player_cells[j] as Player;
                    if (client.Deleted)
                    {
                        if (ServerConfig.IsClearPlayer)
                        {
                            if (player.Deleted_Clear_Tick < ServerConfig.PlayerClearTime)
                                player.Deleted_Clear_Tick++;
                            else
                            {
                                player.Name = "Dead";
                                player.Deleted = true;
                                player.color_index = 112;
                                player.Transverse = 0;
                                player.Longitudinal = 0;
                            }
                        }
                    }
                    player.MonitorBorderCollide();

                    player.Tick(this);
                }

                client.Tick(this);
            }

        }
        #endregion
    }
}
