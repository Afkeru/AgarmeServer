using AgarmeServer.Client;
using AgarmeServer.Entity;
using AgarmeServer.Network;
using AgarmeServer.Others;
using AgarmeServer.Map;
using AgarmeServer.Zeroer;
using System;
using System.Timers;
#pragma warning disable 219,414

namespace AgarmeServer.Map
{
    public class World
    {
        public static uint MaxHandle = 1;
        public UsingLock<object> Cells_Lock;
        public UsingLock<object> Client_Lock;
        public readonly uint Handle=0;
        public uint PresentPlayer = 0;

        public uint FoodAmount = 0;
        public uint VirusAmount = 0;
        private float FoodTick = 0;

        public List<Cell> BoostCell = new List<Cell>();
        public List<uint> BtList = new List<uint>();
        public QuadTree<Cell> quadtree = new QuadTree<Cell>(0, 0, ServerConfig.BoarderWidth, ServerConfig.BoarderHeight);
        public Dictionary<uint,PlayerClient> PlayerList = new Dictionary<uint, PlayerClient>();
        public List<Cell> CellList = new List<Cell>();
        public System.Timers.Timer timer = new System.Timers.Timer();
        public Task World_Tick;

        public World()
        {
            Handle = MaxHandle++;

            Cells_Lock= new UsingLock<object>();
            Client_Lock= new UsingLock<object>();

            timer.Interval = ServerConfig.Tick_Interval;
            timer.Elapsed += new ElapsedEventHandler(Tick);
        }

        public unsafe void Tick(object obj,ElapsedEventArgs args)
        {
            if (Program.server.HasStarted is not true) return;

            if (ServerConfig.isBorderShrink) { ServerConfig.BoarderWidth--; ServerConfig.BoarderHeight--; }

            var r1 = 0.0d;
            var r2 = 0.0d;
            var loop = CellList.Count;

            for (var i = 0; i < loop; i++)
            //foreach(var cell1 in CellList)
            {
                Cell cell1 = CellList[i];

                if (cell1 is null) continue;
                if (cell1.Deleted) continue;

                r1 = cell1.R;

                #region 细胞与边界碰撞
                cell1.MonitorBorderCollide();
                #endregion

                #region 细胞逻辑处理
                //食物
                if (cell1.Type is Constant.TYPE_FOOD)
                {
                    var food = cell1 as Food;
                    FoodAmount++;
                    food.Tick();
                }

                //玩家
                if (cell1.Type is Constant.TYPE_PLAYER)
                {
                    Player player = cell1 as Player;

                    if (player.Client.Deleted is true)
                    {
                        player.Name = "Dead";
                        player.Deleted = true;
                        player.color_index = 112;
                        player.Transverse = 0;
                        player.Longitudinal = 0;

                        continue;
                    }

                    player.Tick(this);
                }

                //病毒
                if (cell1.Type is Constant.TYPE_VIRUS)
                {
                    var virus = cell1 as Virus;
                    VirusAmount++;
                    virus.Tick(this);
                }

                //吐球
                if (cell1.Type is Constant.TYPE_EJECT)
                {
                    var eject = cell1 as Eject;
                    eject.Tick(this);
                }

                //侍从
                if (cell1.Type is Constant.TYPE_BOT)
                {

                }

                //人机
                if (cell1.Type is Constant.TYPE_ROBOT)
                {

                }
                #endregion
            }
            HandlePlayer();

            UpdateFoodAndVirus();

            HandleCells();



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
            for (int i = 0; i < BoostCell.Count; i++)
            //foreach (var cell in BoostCell)
            {
                var cell = BoostCell[i];

                CellList.Add(cell);
                quadtree.Add(cell);

            }
            BoostCell.Clear();

            for (int i = CellList.Count - 1; i >= 0; i--)
            {
                Cell cell = CellList[i];

                if (cell.Deleted)
                {
                    quadtree.Remove(cell);
                    CellList.RemoveAt(i);
                }
            }

            

        }

        private void HandlePlayer()
        {
            for (int i = PlayerList.Count-1; i >=0; i--)
            //foreach(var client in PlayerList.Values)
            {
                PlayerClient client = PlayerList.ElementAt(i).Value;

                if (client is null) { PlayerList.Remove(client.BT); continue; }

                //if (client.Deleted) { PlayerList.Remove(client.BT); continue; }

                client.Tick(this);
            }

        }
        #endregion
    }
}
