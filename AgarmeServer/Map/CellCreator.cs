using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using AgarmeServer.Client;
using AgarmeServer.Entity;
using AgarmeServer.HKObj;
using AgarmeServer.Others;

namespace AgarmeServer.Map
{
    public static class CellCreator
    {
        public static void Generate_Food(int number, World world)
        {
            for (int i = 0; i < number; ++i)
            {
                Food food = new Food();
                //食物生成位置
                food.X = HKRand.Double(0, ServerConfig.BoarderWidth);
                food.Y = HKRand.Double(0, ServerConfig.BoarderHeight);
                //从颜色表中直接读颜色
                food.SetRandomColor(1);
                //初始质量
                food.Size = HKRand.Double(ServerConfig.FoodMinSize, ServerConfig.FoodMaxSize);
                //配置细胞身份证
                food.SetID();
                //设置细胞名称
                food.Name = "";
                //设置细胞类型
                food.Type = Constant.TYPE_FOOD;
                food.FatherCell = null;

                world.Cells.Add(food);
                world.quadtree.Insert(food);
                //world.FoodAmount++;
            }
        }
        public static void Generate_Virus(int number, World world)
        {
            for (int i = 0; i < number; ++i)
            {
                Virus virus = new Virus();
                //食物生成位置
                virus.X = HKRand.Double(0, ServerConfig.BoarderWidth);
                virus.Y = HKRand.Double(0, ServerConfig.BoarderHeight);
                //病毒默认是黄色
                virus.color_index =(byte)(HKColor.colorTable.Length - 1);
                virus.Color = new HKColor(System.Drawing.Color.Yellow.ToArgb());
                //初始质量
                virus.Size = ServerConfig.VirusSize;
                //配置细胞身份证
                virus.SetID();
                //设置细胞名称
                virus.Name = "";
                //设置细胞类型
                virus.Type = Constant.TYPE_VIRUS;
                virus.FatherCell = null;

                world.Cells.Add(virus);
                world.quadtree.Insert(virus);
            }
        }

        public static void Generate_Player(PlayerClient client, World world)
        {
            Player player = new Player(client);
            //出生点
            player.X = HKRand.Double(0, ServerConfig.BoarderWidth);
            player.Y = HKRand.Double(0, ServerConfig.BoarderHeight);
            //方案二随机生成颜色
            player.SetRandomColor(1);

            player.Type = Constant.TYPE_PLAYER;
            //初始质量
            player.Size = ServerConfig.PlayerStartSize;

            player.CollisionC = ServerConfig.PlayerCollisionTime * 0.9;

            client.OwnCells.Add(player.Id, player);

            world.PlayerCells.Add(player);

            world.quadtree.Insert(player);

        }

        public static Player Generate_Player(double x, double y, double size, string name, byte color_index, PlayerClient client, World world)
        {
            Player player = new Player(client);

            player.Type = Constant.TYPE_PLAYER;
            //出生点
            player.X = x;
            player.Y = y;
            //方案二随机生成颜色
            player.color_index = color_index;
            //初始质量
            player.Size = size;

            player.FusionC = 0;

            client.OwnCells.Add(player.Id, player);

            player.CollisionC = ServerConfig.PlayerCollisionTime * 0.9;

            world.BoostCell.Add(player);

            //world.PlayerCells.Add(player);

            //world.quadtree.Insert(player);

            return player;
        }

        public static void Generate_Player_Around_Minion(PlayerClient client, World world)
        {
            Player player = new Player(client);
            //出生点
            player.X = HKRand.Double(client.MyMinion.AverageViewX - 50 * 0.5, client.MyMinion.AverageViewX + 50 * 0.5);
            player.Y = HKRand.Double(client.MyMinion.AverageViewY - 50 * 0.5, client.MyMinion.AverageViewY + 50 * 0.5);
            //方案二随机生成颜色
            player.SetRandomColor(1);

            player.Type = Constant.TYPE_PLAYER;
            //初始质量
            player.Size = ServerConfig.PlayerStartSize;

            player.CollisionC = ServerConfig.PlayerCollisionTime * 0.9;

            world.BoostCell.Add(player);
            //world.PlayerCells.Add(player);

            //world.quadtree.Insert(player);

            client.OwnCells.Add(player.Id, player);

        }

        public static void Generate_Minion(MinionClient client, World world)
        {
            Minion minion = new Minion(client);
            //出生点
            minion.X = HKRand.Double(client.Parent.AverageViewX-350*0.5, client.Parent.AverageViewX + 350 * 0.5);
            minion.Y = HKRand.Double(client.Parent.AverageViewY - 350 * 0.5, client.Parent.AverageViewY+ 350 * 0.5);
            //方案二随机生成颜色
            minion.SetRandomColor(1);

            minion.Type = Constant.TYPE_BOT;
            //初始质量
            minion.Size = ServerConfig.MinionMass;

            minion.CollisionC = ServerConfig.PlayerCollisionTime * 0.9;

            //world.Cells.Add(minion);

            //world.quadtree.Insert(minion);

            world.BoostCell.Add(minion);

            client.OwnCells.Add(minion.Id, minion);

        }

        public static Minion Generate_Minion(double x, double y, double size, string name, byte color_index, MinionClient client, World world)
        {
            Minion minion = new Minion(client);

            minion.Type = Constant.TYPE_BOT;
            //出生点
            minion.X = x;
            minion.Y = y;
            //方案二随机生成颜色
            minion.color_index = color_index;
            //初始质量
            minion.Size = size;

            minion.FusionC = 0;

            client.OwnCells.Add(minion.Id, minion);

            //world.Cells.Add(minion);

            //world.quadtree.Insert(minion);

            minion.CollisionC = ServerConfig.PlayerCollisionTime * 0.9;

            world.BoostCell.Add(minion);

            return minion;
        }
    }
}
