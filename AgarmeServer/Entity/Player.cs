using AgarmeServer.Client;
using AgarmeServer.HKObj;
using AgarmeServer.Map;
using AgarmeServer.Network;
using AgarmeServer.Others;
using AgarmeServer.Zeroer;
using HPSocket.Base;
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

            if (Client.State is not ClientState.Playing) { Deleted = true;return; }

            var client = world.PlayerList[BT];
            var mouse = client.Mouce;
            var r1 = R;
            var r2 = 0.0d;
            var isFussion = false;
            var FussionConstant = ServerConfig.PlayerFusionConstant * ServerConfig.PlayerFusionTime * Size;
            var tempDist = 0.0d;

            if (Size is < 5) {Deleted=true; return; }

            FusionC += 0.4;

            if (CollisionC > 0) CollisionC--;

            if (Size > ServerConfig.PlayerLimitSize) Size = ServerConfig.PlayerLimitSize;

            Name = client.Name;

            //衰减
            Size *= ServerConfig.PlayerSizeRate;

            //移动
            Speed = ServerConfig.PlayerMoveSpeed / Math.Pow(Size, ServerConfig.PlayerMoveSpeedRate);
            if (Client.Focus) MoveTo_ATan(mouse, Speed);
            else MoveTo_ATan(client.LastMouceLoc, Speed);

            client.SplitCount += 1;
            client.ViewX += x;
            client.ViewY += y;
            client.Mass += Size; 

            //横向纵向运动
            CountInertia(ref x, ref y, ref transverse, ref longitudinal, ServerConfig.PlayerSplitSpeedRate);
            CountInertia(ref x, ref y, ref xd, ref yd, ServerConfig.PlayerMoveSpeedRate);

            //自动分裂
            if (Size >= ServerConfig.PlayerMaxSize)
            {
                client.SpaceCount += 1;

                var w = ServerConfig.BoarderWidth;
                var h = ServerConfig.BoarderHeight;

                var rand_point = new HKPoint(HKRand.Double(-w, w), HKRand.Double(-h, h));

                Split(rand_point, ServerConfig.PlayerSplitSpeed * Math.Pow(Size, -0.001), world);
            }

            world.quadtree.Search(Range, (other) =>
            {
                var cell = other;

                if (other.Deleted) return;

                if (other.Id == Id) return;

                r2 = cell.R;

                tempDist = Distance(cell.Cell_Point);

                Client.MergeTick++;

                isFussion = FusionC >= FussionConstant && FusionC >= ServerConfig.PlayerFusionTime * ServerConfig.PlayerFusionConstant * Size;

                //碰撞
                if (cell.Type is Constant.TYPE_PLAYER)
                {
                    var player = cell as Player;
                    if (CollisionC is <= 0 && player.CollisionC is <= 0 && (isFussion is false))
                    {
                        if (IsCollideWith(player, tempDist))
                        {
                            if (client.BT == player.BT)
                            {
                                CollideWith(player);
                                world.quadtree.Update(this);
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

                        if (client.BT == player.BT && isFussion && CollisionC is <= 0 && player.CollisionC is <= 0)
                        {
                            FusionC = ServerConfig.PlayerFussionStart;

                            Size += player.Size;

                            player.Deleted = true;

                            world.quadtree.Remove(player);

                            return;
                        }
                    }
                    //吞噬
                    if (Size >= cell.Size * ServerConfig.DevourSizeDegree)
                    {
                        if (cell.Type is Constant.TYPE_PLAYER)
                        {
                            var player = cell as Player;
                            if (client.BT == player.BT) return;
                        }
                        if (cell.Type is Constant.TYPE_BOT)
                        {
                            var minion = cell as Minion;
                            //最后一个细胞
                            if(minion.Client.OwnCells.Count is <=1)
                            {
                                minion.Client.Parent.Tab = false;
                                minion.Client.Die = true;
                            }
                        }
                        if (cell.Type is Constant.TYPE_EJECT)
                        {
                            if (CollisionC is <= 0 && isFussion)
                            {
                                Size += cell.Size;
                                cell.Deleted = true;
                                cell.EatenBy = this;
                                return;
                            }
                        }

                        Size += cell.Size;
                        cell.Deleted = true;
                        cell.EatenBy = this;

                        //扎病毒
                        if (cell.Type is Constant.TYPE_VIRUS)
                        {
                            Pop(world);
                        }

                    }

                }
            });

            //吐球
            #region Eject
            if (Client.Focus)
            {
                if (client.Eject)
                {
                    if (Client.Macro)
                    {
                        if (Size >= ServerConfig.PlayerMinEjectSize)
                        {
                            if (Size > ServerConfig.PlayerEjectLose * 11)
                            {
                                for (int jj = 0; jj < 10; ++jj)
                                {
                                    Size -= ServerConfig.PlayerEjectLose;
                                    var dev_position = CalDeviation(mouse, r / 11, r * 1.1);
                                    Eject temp_eject = new Eject(mouse);
                                    temp_eject.color_index = color_index;
                                    temp_eject.x = X + dev_position.X;
                                    temp_eject.y = Y + dev_position.Y;
                                    temp_eject.MoveTo(Client.Mouce, ServerConfig.PlayerEjectSpeed * Math.Pow(Size, 0.1), ref temp_eject.transverse, ref temp_eject.longitudinal);
                                    temp_eject.DesLocation = Client.Mouce;
                                    temp_eject.Type = Constant.TYPE_EJECT;
                                    EjectC = 0;
                                    world.BoostCell.Add(temp_eject);
                                }
                            }

                        }
                    }
                    else if (Size >= ServerConfig.PlayerMinEjectSize)
                    {
                        if (EjectC >= ServerConfig.PlayerEjectInterval)
                        {
                            Size -= ServerConfig.PlayerEjectLose;
                            var dev_position = CalDeviation(Client.Mouce, r / 11, r);
                            Eject temp_eject = new Eject(Client.Mouce);
                            temp_eject.color_index = color_index;
                            temp_eject.x = X + dev_position.X;
                            temp_eject.y = Y + dev_position.Y;
                            temp_eject.MoveTo(Client.Mouce, ServerConfig.PlayerEjectSpeed * Math.Pow(Size, 0.1), ref temp_eject.transverse, ref temp_eject.longitudinal);
                            EjectC = 0;
                            temp_eject.DesLocation = Client.Mouce;
                            temp_eject.Type = Constant.TYPE_EJECT;
                            world.Cells.Add(temp_eject);
                            world.quadtree.Insert(temp_eject);
                        }
                        else
                        {
                            EjectC += 1;
                        }
                    }
                }
            }
            #endregion

            world.quadtree.Update(this);
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
            if (Client.OwnCells.Count>=ServerConfig.PlayerSplitLimit) return;
            if (Size < ServerConfig.PlayerMinSplitSize) return;

            var lost_size = Size * 0.5;
            Size -= lost_size;
            CollisionC = ServerConfig.PlayerCollisionTime;
            transverse = 0;
            longitudinal = 0;

            var player = CellCreator.Generate_Player(X, Y, lost_size, Name, color_index, Client, world);

            var temp_point = player.CalDeviation(Des, 0, r * ServerConfig.CollisionDeviationConstant);
            player.x += temp_point.X;
            player.y += temp_point.Y;
            player.FusionC = 0;
            player.CollisionC = ServerConfig.PlayerCollisionTime * 0.9;
            FusionC = 0;

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
                    if (Size > size && Size>ServerConfig.PlayerMinSplitSize)
                    {
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
        }
    }
}
