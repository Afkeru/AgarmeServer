using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using AgarmeServer.Client;
using AgarmeServer.Entity;
using AgarmeServer.Others;
using HPSocket.Base;

namespace AgarmeServer.Map
{
    /// <summary>
    /// 世界由MatchManager类进行管理
    /// </summary>
    public class MatchManager
    {
        public World world;
        public uint WorldMaxPlayer;
        public MatchManager(uint max_player = 15)
        {
            world = new World();
            WorldMaxPlayer = max_player;
            IniEntity();
        }
        public MatchManager(World _world, uint max_player = 15)
        {
            world = _world;
            WorldMaxPlayer = max_player;
        }
        public void PlayerIn(PlayerClient player)
        {
            if(world.PresentPlayer >= WorldMaxPlayer)
            {
                //世界玩家数量超过最大数量
            }
            else
            {
                //世界玩家数量未超过最大数量
                world.PresentPlayer++;
                world.PlayerList.Add(player.BT, player);
            }
        }
        public bool PlayerOut(uint bt)
        {
            if (world.PlayerList.ContainsKey(bt) is false) { return false; }

            PlayerClient player_client = world.PlayerList[bt];

            var client_keys = player_client.OwnCells.Keys.ToArray();
            for (var i=0;i < client_keys.Length;++i)
            {
                var player = player_client.OwnCells[client_keys[i]];
                player.Deleted = true;
            }

            player_client.OwnCells.Clear();

            player_client.State = ClientState.Disconnected;

            player_client.Deleted = true;

            Program.server.Disconnect((IntPtr)bt);


            player_client.BT = 0;

            player_client = null;

            world.PresentPlayer--;
            return true;
        }

        public void ClearWorld()
        {
            Program.world_manager.world.Stop();

            world.PlayerList.Clear();

            world.Cells.Clear();

            world.PlayerCells.Clear();

            world.quadtree= new QuadTree<Cell>(new System.Drawing.RectangleF(0, 0, ServerConfig.BoarderWidth, ServerConfig.BoarderHeight), 16, 16, null); ;
            world.PresentPlayer = 0;

            IniEntity();

            world.Start();
        }

        public void FreshWorld()
        {
            world.Cells.Clear();
            world.PlayerCells.Clear();
            world.quadtree=new QuadTree<Cell>(new System.Drawing.RectangleF(0, 0, ServerConfig.BoarderWidth, ServerConfig.BoarderHeight), 16, 16, null);
        }

        public void IniEntity()
        {
            CellCreator.Generate_Food(ServerConfig.AFood, world);

            CellCreator.Generate_Virus(ServerConfig.AVirus, world);
        }
    }
}
