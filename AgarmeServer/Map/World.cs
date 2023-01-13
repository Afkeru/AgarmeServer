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

        public List<Cell> BoostCell = new List<Cell>();
        public List<uint> BtList = new List<uint>();
        public QuadTree<Cell> quadtree = new QuadTree<Cell>(new System.Drawing.RectangleF(0,0,ServerConfig.BoarderWidth,ServerConfig.BoarderHeight),16, 16, null);
        public Dictionary<uint,PlayerClient> PlayerList = new Dictionary<uint, PlayerClient>();
        public List<Cell> Cells = new List<Cell>();
        public List<Cell> PlayerCells = new List<Cell>();
        public Ticker ticker = new Ticker(40);
        private Stopwatch stopWatch = new Stopwatch();


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

            var leng = PlayerCells.Count;

            for (int i = 0; i < leng; i++)
            {
                var player = PlayerCells[i] as Player;

                if (player.Client.Deleted)
                {
                    player.Name = "Dead";
                    player.Deleted = true;
                    player.color_index = 112;
                    player.Transverse = 0;
                    player.Longitudinal = 0;
                }
                player.MonitorBorderCollide();

                player.Tick(this);

            }

            var len = Cells.Count;
            for (var i = 0; i < len; i++)
            {
                Cell cell = Cells[i];

                r1 = cell.R;

                #region 细胞与边界碰撞
                cell.MonitorBorderCollide();
                #endregion

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
            }

            HandlePlayer();

            UpdateFoodAndVirus();

            HandleCells();

            var keys = PlayerList.Keys.ToArray();
            for (int i = 0, l = keys.Length; i < l; i++)
            {
                PlayerClient client = PlayerList[keys[i]];

                //if (client.Deleted) { PlayerList.Remove(client.BT); }
                //if (client is null) { PlayerList.Remove(client.BT); }

                if (client.SplitQueue.Count is not 0) client.SplitQueue.Dequeue();
            }

            avargateTickTime = stopWatch.Elapsed.TotalMilliseconds;
            stopWatch.Reset();
        }
        #region 辅助函数
        private void UpdateFoodAndVirus()
        {
            ////刷新病毒
            if (VirusAmount < ServerConfig.AVirus)
                CellCreator.Generate_Virus(1, this);

            //刷新食物
            if (FoodAmount < ServerConfig.AFood)
            {
                if (FoodTick >= ServerConfig.FoodAppearsCounter)
                {
                    CellCreator.Generate_Food(ServerConfig.FoodAppearsAmount, this);
                    FoodTick = 0;
                }
                else FoodTick++;
            }

            VirusAmount = 0;
            FoodAmount  = 0;

        }

        private void HandleCells()
        {
            for (int i = 0; i < BoostCell.Count;)
            {
                var cell = BoostCell[i];

                if (cell.Type is Constant.TYPE_PLAYER) PlayerCells.Add(cell);
                else Cells.Add(cell);

                quadtree.Insert(cell);

                i++;
            }
            BoostCell.Clear();

            for (int i = Cells.Count-1; i >=0; i--)
            {
                Cell cell = Cells[i];

                if(cell is null) continue; 

                if (cell.Deleted)
                {
                    switch (cell.Type)
                    {
                        case Constant.TYPE_BOT:
                            {
                                var minion = cell as Minion;
                                minion.Client.OwnCells.Remove(minion.Id);
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }

                    Cells.RemoveAt(i);
                    quadtree.Remove(cell);
                }

            }
            for (int i = PlayerCells.Count - 1; i>=0; i--)
            {
                var player = PlayerCells[i] as Player;

                if (player is null) continue;

                if (player.Deleted)
                {
                    player.Client.OwnCells.Remove(player.Id);
                    PlayerCells.RemoveAt(i);
                    quadtree.Remove(player);
                }
            }
        }

        private void HandlePlayer()
        {
            var keys = PlayerList.Keys.ToArray();
            for (int i = 0, l = keys.Length; i < l; i++)
            {
                PlayerClient client = PlayerList[keys[i]];

                if (client.Deleted) { PlayerList.Remove(client.BT); }
                if (client is null) { PlayerList.Remove(client.BT); }

                client.Tick(this);
            }

        }
        #endregion
    }
}
