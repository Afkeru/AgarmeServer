using AgarmeServer.Client;
using AgarmeServer.HKObj;
using AgarmeServer.Map;
using AgarmeServer.Network;
using AgarmeServer.Others;
using AgarmeServer.Zeroer;
using HPSocket.Base;
using System.Drawing;
using System.Numerics;
using System.Text;
using Constant = AgarmeServer.Others.Constant;
#pragma warning disable 0219, 8981

namespace AgarmeServer.Entity
{
    public class Player:Cell
    {
        public double EjectDestinyX = 0, EjectDestinyY = 0;//保存吐球鼠标的位置，利于记录病毒分裂方位
        public uint BT;
        //以C为结尾的变量都表示计时
        public double CollisionC = 0;//碰撞计时
        public double FusionC = 0;//合体计时
        public double EjectC = 10;//吐球间隔计时
        public double LossC = 0;//质量衰减间隔计时
        public double ClearTimer = 0;//玩家清除间隔计时
        public PlayerClient Client;

        public Player(PlayerClient client)
        {
            Client = client;

            Type = Constant.TYPE_PLAYER;
            SetID();
            BT = client.BT;
            Name = client.Name;
            CollisionC = ServerConfig.PlayerCollisionTime * 0.9;

        }

        public void Tick(World world)
        {
            if (Deleted) return;
            if (Client.State is ClientState.Playing)
            {
                var mouse = Client.Mouce;
                var r1 = R;
                var r2 = 0.0d;
                var isFussion = false;
                var FussionConstant = ServerConfig.PlayerFusionConstant * ServerConfig.PlayerFusionTime * Size;
                var tempDist = 0.0d;

                if (Size is < 1) { Size = 0; Deleted=true; return; }

                FusionC++;

                if (CollisionC > 0) CollisionC--;

                if (Size > ServerConfig.PlayerLimitSize) Size = ServerConfig.PlayerLimitSize;

                Name = Client.Name;

                //衰减
                Size *= ServerConfig.PlayerSizeRate;

                //移动
                Speed = ServerConfig.PlayerMoveSpeed / Math.Pow(Size, ServerConfig.PlayerMoveSpeedRate);
                MoveTo_ATan(mouse, Speed);

                //吐球
                if (Client.Eject)
                {
                    if (Size >= ServerConfig.PlayerMinEjectSize)
                    {
                        if (ServerConfig.PlayerEjectInterval is 0)
                        {
                            if (Size > ServerConfig.PlayerEjectLose * 5)
                            {
                                for (int jj = 0; jj < 5; ++jj)
                                {
                                    Size -= ServerConfig.PlayerEjectLose;
                                    var dev_position = CalDeviation(Client.Mouce, r / 11, r*1.1);
                                    Eject temp_eject = new Eject(Client.Mouce);
                                    temp_eject.color_index = color_index;
                                    temp_eject.x = X + dev_position.X;
                                    temp_eject.y = Y + dev_position.Y;

                                    temp_eject.MoveTo(Client.Mouce, ServerConfig.PlayerEjectSpeed * Math.Pow(Size, 0.1), ref temp_eject.transverse, ref temp_eject.longitudinal);
                                    temp_eject.DesLocation = Client.Mouce;
                                    EjectC = 0;
                                    world.BoostCell.Add(temp_eject);
                                }
                            }
                        }
                        else
                        {
                            if (EjectC >= ServerConfig.PlayerEjectInterval)
                            {
                                if (Size > ServerConfig.PlayerEjectLose)
                                {
                                    Size -= ServerConfig.PlayerEjectLose;
                                    var dev_position =CalDeviation(Client.Mouce, r / 11, r);
                                    Eject temp_eject = new Eject(Client.Mouce);
                                    temp_eject.color_index = color_index;
                                    temp_eject.x = X + dev_position.X;
                                    temp_eject.y = Y + dev_position.Y;
                                    temp_eject.MoveTo(Client.Mouce, ServerConfig.PlayerEjectSpeed * Math.Pow(Size, 0.1), ref temp_eject.transverse, ref temp_eject.longitudinal);
                                    EjectC = 0;
                                    temp_eject.DesLocation = Client.Mouce;
                                    world.BoostCell.Add(temp_eject);
                                }
                            }
                            else
                            {
                                EjectC += 1;
                            }
                        }
                    }
                }

                Client.SplitCount++;
                Client.ViewX += x;
                Client.ViewY += y;
                Client.Mass += Size;

                //横向纵向运动
                CountInertia(ref x, ref y, ref transverse, ref longitudinal, ServerConfig.PlayerSplitSpeedRate);
                CountInertia(ref x, ref y, ref xd, ref yd, ServerConfig.PlayerMoveSpeedRate);

                //主动分裂
                if (Client.Split)
                {
                    //if (Client.OwnCells.Count<ServerConfig.PlayerSplitLimit)
                    if(Client.SplitCheck())
                    {
                        if (Size*2 >= ServerConfig.PlayerMinSplitSize)
                        {
                            Client.SpaceCount += 1;
                            Split(mouse, ServerConfig.PlayerSplitSpeed * Math.Pow(Size, -0.001), world);
                        }
                    }
                }

                //自动分裂
                if (Size >= ServerConfig.PlayerMaxSize)
                {
                    /*是否还有剩余空间分裂*/
                    if (Client.SplitCheck())
                    {
                        Client.SpaceCount += 1;

                        var w = ServerConfig.BoarderWidth;
                        var h = ServerConfig.BoarderHeight;

                        var rand_point = new HKPoint(HKRand.Double(-w, w), HKRand.Double(-h, h));

                        Split(rand_point, ServerConfig.PlayerSplitSpeed * Math.Pow(Size, -0.001), world);
                    }
                }

                var val = world.quadtree.GetObjects(Rect).ToArray();
                var len = val.Length;
                //foreach (var cell in val)
                for (var i=0;i< len; i++)
                {
                    var cell = val[i];
                    if (cell.Deleted) continue;
                    if (this == cell) continue;

                    r2 = cell.R;

                    tempDist = Distance(cell.Cell_Point);
                    if (tempDist > (r1 + r2) * 1.05) continue;

                    isFussion = FusionC >= FussionConstant && FusionC >= ServerConfig.PlayerFusionTime * ServerConfig.PlayerFusionConstant * Size;

                    //碰撞
                    if (cell.Type is Constant.TYPE_PLAYER)
                    {
                        var player = cell as Player;
                        if (CollisionC is <= 0 && player.CollisionC is <=0 &&  isFussion is false)
                        {
                            if (IsCollideWith(player, tempDist))
                            {
                                if (Client.BT == player.BT)
                                {
                                    CollideWith(player);
                                    world.quadtree.Move(cell);
                                }
                            }
                        }
                        
                      
                    }
                    if (tempDist <= r1 - r2 * ServerConfig.CoverageDegree)
                    {
                        //融合
                        if (cell.Type is Constant.TYPE_PLAYER)
                        {
                            var player = cell as Player;

                            if (Client.BT == player.BT && isFussion && CollisionC is <= 0 && player.CollisionC is <= 0)
                            {
                                FusionC += player.FusionC * ServerConfig.PlayerFussionStart;

                                Size += player.Size;

                                Client.OwnCells.Remove(player.Id);

                                player.Deleted= true;

                                world.quadtree.Remove(player);

                                continue;
                            }
                        }
                        //吞噬
                        if (Size >= cell.Size * ServerConfig.DevourSizeDegree)
                        {
                           

                            if (cell.Type is Constant.TYPE_PLAYER)
                            {
                                var player = cell as Player;
                                if (Client.BT == player.Client.BT) continue; else player.Client.OwnCells.Remove(player.Id);
                            }
                            
                            Size += cell.Size;
                            cell.Deleted = true;
                            cell.EatenBy = this;

                            //扎病毒
                            if(cell.Type is Constant.TYPE_VIRUS)
                            {
                                Pop(world);
                            }

                        }

                    }
                }
                world.quadtree.Move(this);
            }
        }

        public unsafe void Serialize(IWritableBuffer wb, PlayerClient client,World world, ref uint len)
        {
            if(Client.BT == client.BT)
            {
                *(byte*)wb.WriteUndefined(1) = Type;
                *(float*)wb.WriteUndefined(4) = (float)X;
                *(float*)wb.WriteUndefined(4) = (float)Y;
                *(float*)wb.WriteUndefined(4) = (float)R;
                *(uint*)wb.WriteUndefined(4) = Id;
                *(ushort*)wb.WriteUndefined(2) = (ushort)Client.BT;
                byte[] namebyte = Encoding.UTF8.GetBytes(Name);
                *(ushort*)wb.WriteUndefined(2) = (ushort)namebyte.Length;
                if (namebyte.Length != 0)
                {
                    var temp = wb.WriteUndefined(namebyte.Length);
                    fixed (byte* ptr = &namebyte[0])
                    {
                        for (int j = 0; j < namebyte.Length; j++)
                        {
                            *((byte*)temp.value + j) = *(ptr + j);
                        }
                        *(byte*)temp.value = *ptr;
                    }
                }


                len++;
                world.BtList.Add(Client.BT);

            }
            else
            {
                if(client.CheckOutInSight(this) || client.SetSpectateSight(this))
                {
                    *(byte*)wb.WriteUndefined(1) = Type;
                    *(float*)wb.WriteUndefined(4) = (float)X;
                    *(float*)wb.WriteUndefined(4) = (float)Y;
                    *(float*)wb.WriteUndefined(4) = (float)R;
                    *(uint*)wb.WriteUndefined(4) = Id;
                    *(ushort*)wb.WriteUndefined(2) = (ushort)Client.BT;
                    byte[] namebyte = Encoding.UTF8.GetBytes(Name);
                    *(ushort*)wb.WriteUndefined(2) = (ushort)namebyte.Length;
                    if (namebyte.Length != 0)
                    {
                        var temp = wb.WriteUndefined(namebyte.Length);
                        fixed (byte* ptr = &namebyte[0])
                        {
                            for (int j = 0; j < namebyte.Length; j++)
                            {
                                *(temp.value + j) = *(ptr + j);
                            }
                            *temp.value = *ptr;
                        }
                    }
                    len++;
                    world.BtList.Add(Client.BT);
                }
            }
        }
   
        public void Split(HKPoint Des, double speed,World world)
        {
            var lost_size = Size * 0.5;
            Size -= lost_size;
            CollisionC = ServerConfig.PlayerCollisionTime;
            transverse = 0;
            longitudinal = 0;

            //Player player = new Player(Client);

            //player.X = X;
            //player.Y = Y;
            ////方案二随机生成颜色
            //player.color_index = color_index;
            //初始质量
            //player.Size = lost_size;

            //player.FusionC = ServerConfig.PlayerCollisionTime;

            //world.CellList.Add(player);

            //world.quadtree.Add(player);

            //player.CollisionC = ServerConfig.PlayerCollisionTime * 0.9;

            //Client.OwnCells.Add(player.Id, player);


            var player = CellCreator.Generate_Player(X, Y, lost_size, Name, color_index, Client, world);

            var temp_point = player.CalDeviation(Des, 0, r * ServerConfig.CollisionDeviationConstant);
            player.x += temp_point.X;
            player.y += temp_point.Y;
            player.FusionC = 0;
            player.CollisionC = ServerConfig.PlayerCollisionTime*0.9;
            FusionC =0;

            player.MoveTo(Des, speed, ref player.transverse, ref player.longitudinal);
        }

        public void Pop(World world)
        {
            var rest_split_count =ServerConfig.PlayerSplitLimit- Client.OwnCells.Count;
            var explode =(int) (rest_split_count*0.3);
            var avg_angle = 360d / explode * Math.PI / 180;
            var size = ServerConfig.PopSplit? Size / explode: ServerConfig.PlayerMinSplitSize * 0.5;
            for (int j = 0; j < explode; j++)
            {
                //var size = HKRand.Double(Size * 0.1, Size * 0.2);
                //var size = ServerConfig.PlayerMinSplitSize*0.5;
                if (Client.OwnCells.Count < ServerConfig.PlayerSplitLimit)//是否还有剩余空位
                {
                    /*判断细胞质量是否达到最小分裂质量*/
                    if (Size > size)
                    {
                        //var angle = HKRand.Double() * 2 * Math.PI;
                        var angle = avg_angle*j;
                        Client.SpaceCount += 1;
                        Size -= size;

                        var player = CellCreator.Generate_Player(X, Y, size, Name, color_index, Client, world);
                        CollisionC = 36;
                        FusionC = ServerConfig.PlayerFussionStart;
                        player.CollisionC = 36;
                        player.FusionC = ServerConfig.PlayerFussionStart;
                        player.MoveTo(angle, ServerConfig.VirusExplodeSpeed, ref player.transverse, ref player.longitudinal);

                    }

                }
                else break;
            }
            //var splits = DirstributeCellMass(this);
            //CollisionC = 4;
            //FusionC = ServerConfig.PlayerFussionStart;
            //for (int i = 0, l = splits.Count; i < l; i++)
            //{
            //    var angle = HKRand.Int(0,360) *Math.PI /180;

            //    if (Client.SplitCheck())
            //    {
            //        var size = Math.Sqrt(splits[i] * 100);
            //        var player = CellCreator.Generate_Player(X+HKRand.Double(-r*0.1,r*0.1), Y + HKRand.Double(-r*0.1, r*0.1), size, Name, color_index, Client, world);
            //        Size -= size;
            //        player.CollisionC = 10;
            //        player.FusionC = 0;

            //        player.MoveTo(angle, Speed, ref player.transverse, ref player.longitudinal);
            //    }

            //}
        }
    }
}
