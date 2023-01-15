using AgarmeServer.Client;
using AgarmeServer.HKObj;
using AgarmeServer.Map;
using AgarmeServer.Zeroer;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Constant = AgarmeServer.Others.Constant;

namespace AgarmeServer.Entity
{
    public class Minion:Cell, IEquatable<Minion>,IDisposable
    {
        public double EjectDestinyX = 0, EjectDestinyY = 0;//保存吐球鼠标的位置，利于记录病毒分裂方位
        public uint Number;
        public double CollisionC = 0;//碰撞计时
        public double FusionC = 0;//合体计时
        public double EjectC = 10;//吐球间隔计时
        public double LossC = 0;//质量衰减间隔计时
        public double ClearTimer = 0;//清除间隔计时
        public double Deleted_Clear_Tick = 0;//死亡清除计时
        private double Biggest_Cell_Eject_Tick = 0;//最大细胞吐球计时
        private double Push_X = 0, Push_Y = 0;
        private bool Poped = false;//是否炸了病毒
        public MinionClient Client;

        public Minion(MinionClient client)
        {
            Client = client;

            Type = Constant.TYPE_BOT;

            SetID();

            Number = client.Number;

            Name = client.Name;

            CollisionC = ServerConfig.PlayerCollisionTime * 0.9;
        }
        public void Tick(World world)
        {
            if (Deleted) return;

            if (Client.Parent.State is not ClientState.Playing) { Deleted = true; return; }

            if (Size < 1) return;
            var mouse = Client.Parent.Mouce;
            var r1 = R;
            var r2 = 0.0d;
            var isFussion = false;
            var FussionConstant = ServerConfig.PlayerFusionConstant * ServerConfig.PlayerFusionTime * Math.Max(Size, 64);
            var tempDist = 0.0d;

            FusionC += 0.4;

            if (CollisionC > 0)
                CollisionC--;
            else
                Poped = false;

            if (Size > ServerConfig.PlayerLimitSize) Size = ServerConfig.PlayerLimitSize;

            //衰减
            Size *= ServerConfig.PlayerSizeRate;

            //移动
            Speed = ServerConfig.PlayerMoveSpeed / Math.Pow(Size, ServerConfig.PlayerMoveSpeedRate);
            MoveTo_ATan(mouse, Speed);


            //更新最大的细胞质量
            Client.UpdateBiggestCell(this);

            Client.SplitCount += 1;
            Client.ViewX += x;
            Client.ViewY += y;
            Client.Mass += Size;

            //横向纵向运动
            CountComponent(ref x, ref y, ref Push_X, ref Push_Y, 0.7);
            CountInertia(ref x, ref y, ref transverse, ref longitudinal, ServerConfig.PlayerSplitSpeedRate);
            CountInertia(ref x, ref y, ref xd, ref yd, ServerConfig.PlayerMoveSpeedRate);

            //更新最大细胞的位置(Solotrick需要)
            if (ServerConfig.IsSolotrick)
            {
                if (CollisionC is > 0 && Client.Parent.Eject && Poped is false)
                {
                    if (Client.CurrentBiggestCell.Id == Id)
                        MoveTo_ATan(Client.Parent.Mouce, Speed);
                    else
                        MoveTo_ATan(Client.CurrentBiggestCell.CellPoint(Client.Parent.Mouce.Symmetry(Client.CurrentBiggestCell.Cell_Point)), Speed * 1.2);//2.2
                }
            }

            //自动分裂
            if (ServerConfig.IsAutoSplit)
                if (Size >= ServerConfig.PlayerMaxSize)
                {
                    Client.SpaceCount += 1;

                    var w = ServerConfig.BoarderWidth;
                    var h = ServerConfig.BoarderHeight;

                    var rand_point = new HKPoint(HKRand.Double(-w, w), HKRand.Double(-h, h));

                    Split(rand_point, ServerConfig.PlayerSplitSpeed * Math.Pow(Size, -0.001), world);
                }

            //吐球
            #region Eject
            if (Client.Focus)
            {
                if (Client.Parent.Eject)
                {
                    if (Client.Parent.Macro)
                        if (Size >= ServerConfig.PlayerMinEjectSize)
                            for (var i = 0; i < 25; ++i)
                                if (Size > ServerConfig.PlayerEjectLose)
                                    Minion_Eject(Client.Parent.Mouce, world);
                                else break;
                        else { }
                    else if (Size >= ServerConfig.PlayerMinEjectSize)
                        if (EjectC >= ServerConfig.PlayerEjectInterval)
                        { Minion_Eject(Client.Parent.Mouce, world); EjectC = 0; }
                        else
                            EjectC += 1;
                }
            }
            #endregion

            world.quadtree.Search(Range, (other) =>
            {
                var cell = other;

                if (other.Deleted) return;

                if (other.Id == Id) return;

                r2 = cell.R;

                tempDist = Distance(cell.Cell_Point);

                if (tempDist > (r + r2) * 1.1) return;

                Client.MergeTick++;

                isFussion = FusionC >= FussionConstant && FusionC >= ServerConfig.PlayerFusionTime * ServerConfig.PlayerFusionConstant * Math.Max(Size, 64);

                //碰撞
                if (cell.Type is Constant.TYPE_BOT)
                {
                    var minion = cell as Minion;
                    if (ServerConfig.IsSolotrick)//Solotrick部分
                        if (Id == Client.CurrentBiggestCell.Id)
                            if (CollisionC <= ServerConfig.PlayerCollisionTime * 0.15 && (isFussion is false))
                                ResolveCollision(minion, tempDist, world);
                            else { }
                        else if (CollisionC <= 0 && minion.CollisionC <= 0 && (isFussion is false))
                            ResolveCollision(minion, tempDist, world);
                        else { }
                    else if (CollisionC <= 0 && minion.CollisionC <= 0 && (isFussion is false))
                        ResolveCollision(minion, tempDist, world);

                    //交换大小细胞质量(Solotrick关键代码)
                    if (ServerConfig.IsSolotrick)
                       if(Client.Parent.Eject)
                            if (CollisionC > 0 && minion.CollisionC > 0 && Id != Client.CurrentBiggestCell.Id && minion.Id != Client.CurrentBiggestCell.Id && !Poped)
                                if (ServerConfig.IsSolotrick)
                                    if (Size <= 256 && Client.OwnCells.Count >= 32)
                                    {
                                        var m = r + minion.r - tempDist;
                                        var M = Size + minion.Size;
                                        var aM = minion.Size / M;
                                        var bM = Size / M;
                                        if (m is not 0)
                                            if (Size >= minion.Size)
                                                MoveTo_ATan_Distance(minion.CellPoint(Cell_Point), Math.Min(m, r) * aM * 0.35);
                                            else
                                                minion.MoveTo_ATan_Distance(CellPoint(minion.Cell_Point), Math.Min(m, r) * bM * 0.2);
                                    }

                }

                if (tempDist <= r1 - r2 * ServerConfig.CoverageDegree)
                {
                    //融合
                    if (cell.Type is Constant.TYPE_BOT)
                    {
                        var minion = cell as Minion;

                        if (Client.Number == minion.Client.Number && isFussion && CollisionC is <= 0 && minion.CollisionC is <= 0)
                        {
                            FusionC = ServerConfig.PlayerFussionStart;

                            Size += minion.Size;
                            minion.Deleted = true;
                        }
                    }
                    //吞噬
                    if (Size >= cell.Size * ServerConfig.DevourSizeDegree)
                    {
                        switch (cell.Type)
                        {
                            case Constant.TYPE_BOT:
                                {
                                    var minion = cell as Minion;
                                    //是侍从自己的的细胞
                                    if (minion.Client.Number == Client.Number)
                                    {
                                        //...
                                        break;
                                    }

                                    if (Client.OwnCells.Count is 1)
                                        Client.OnDead();

                                    Eat(cell);
                                    break;
                                }
                            case Constant.TYPE_VIRUS:
                                {
                                    Pop(world);
                                    Eat(cell);
                                    break;
                                }
                            case Constant.TYPE_EJECT:
                                {
                                    var eject = cell as Eject;
                                    if (eject.CanEat())
                                    {
                                        if (ServerConfig.IsSolotrick)
                                        {
                                            var biggest = Id == Client.CurrentBiggestCell.Id;
                                            if (CollisionC > 0)
                                                if (biggest && FatherCell is not null)
                                                {
                                                    Eat(cell);
                                                    eject.Size = 0;
                                                    break;
                                                }
                                                else
                                                {
                                                    if (CollisionC < ServerConfig.PlayerCollisionTime * 0.9)
                                                    {
                                                        Eat(cell);
                                                        eject.Size = 0;
                                                        break;
                                                    }
                                                }
                                            else
                                            {
                                                Eat(cell);
                                                eject.Size = 0;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            Eat(eject);
                                            eject.Size = 0;
                                            break;
                                        }
                                    }
                                    break;
                                }
                            case Constant.TYPE_PLAYER:
                                {
                                    var player = cell as Player;
                                    //是自己的主人
                                    if (player.BT == Client.Parent.BT)
                                    {
                                        //......
                                    }

                                    if (player.Client.OwnCells.Count is 1)
                                        player.Client.OnDead();
                                    Eat(cell);
                                    break;
                                }
                            default:
                                {
                                    Eat(cell);
                                    break;
                                }
                        }

                    }

                }
            });
            world.quadtree.Update(this);
        }

        public unsafe void Serialize(IWritableBuffer wb, PlayerClient client,World world, ref uint len)
        {
            if (Client.Number == client.BT)
            {
                *(byte*)wb.WriteUndefined(1) = Type;
                *(float*)wb.WriteUndefined(4) = (float)X;
                *(float*)wb.WriteUndefined(4) = (float)Y;
                *(float*)wb.WriteUndefined(4) = (float)R;
                *(ushort*)wb.WriteUndefined(2) =(ushort)Id;
                *(ushort*)wb.WriteUndefined(2) = (ushort)Client.Number;
                byte[] namebyte = Encoding.UTF8.GetBytes(Name);
                *(ushort*)wb.WriteUndefined(2) = (ushort)namebyte.Length;
                if (namebyte.Length is not 0)
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

            }
            else
            {
                if (client.CheckOutInSight(this) || client.SetSpectateSight(this))
                {
                    *(byte*)wb.WriteUndefined(1) = Type;
                    *(float*)wb.WriteUndefined(4) = (float)X;
                    *(float*)wb.WriteUndefined(4) = (float)Y;
                    *(float*)wb.WriteUndefined(4) = (float)R;
                    *(ushort*)wb.WriteUndefined(2) = (ushort)Id;
                    *(ushort*)wb.WriteUndefined(2) = (ushort)Client.Number;
                    byte[] namebyte = Encoding.UTF8.GetBytes(Name);
                    *(ushort*)wb.WriteUndefined(2) = (ushort)namebyte.Length;
                    if (namebyte.Length is not 0)
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
                }
            }
            
        }

        public void Split(HKPoint Des, double speed, World world)
        {
            if (Client.OwnCells.Count >= ServerConfig.PlayerSplitLimit) return;

            if (Size < ServerConfig.PlayerMinSplitSize) return;

            var lost_size = Size * 0.5;

            Size -= lost_size;

            FusionC = 0;

            CollisionC = ServerConfig.PlayerCollisionTime;

            var minion = CellCreator.Generate_Minion(X, Y, lost_size, Name, color_index, Client, world);

            var deviation_vec = minion.CalDeviation(Des, 0, r * ServerConfig.CollisionDeviationConstant);

            minion.x += deviation_vec.X;

            minion.y += deviation_vec.Y;

            minion.FusionC = 0;

            minion.CollisionC = ServerConfig.PlayerCollisionTime * 0.9;

            minion.MoveTo(Des, speed, ref minion.transverse, ref minion.longitudinal);
        }

        public bool Split_Edited(HKPoint Des, double speed, World world)
        {
            if (Deleted) return false;

            if (Client.OwnCells.Count >= ServerConfig.PlayerSplitLimit) return false;

            if (Size < ServerConfig.PlayerMinSplitSize) return false;

            FusionC = 0;

            CollisionC = ServerConfig.PlayerCollisionTime;

            // Get angle
            var deltaY = Des.Y - Y;
            var deltaX = Des.X - X;
            var angle = Math.Atan2(deltaX, deltaY);

            // Get starting position
            var size = r * ServerConfig.CollisionDeviationConstant;
            //var startPos = new HKPoint(X + (size * Math.Sin(angle)), Y + (size * Math.Cos(angle)));
            var startPos = new HKPoint(X, Y);
            var newMass = Size * 0.5;

            //update fathercell mass
            Size *= 0.5;

            //creat cell
            var minion = CellCreator.Generate_Minion(startPos.X, startPos.Y, newMass, Name, color_index, Client, world);
            minion.MoveTo_ATan_Distance(Des, size, ref minion.Push_X, ref minion.Push_Y);
            minion.Number = Number;
            minion.Client = Client;

            minion.MoveTo_ATan_Distance(Des, Math.Max(1.5, speed), ref minion.transverse, ref minion.longitudinal);
            return true;
        }

        //public void Split_Edited(HKPoint Des, double speed, World world)
        //{
        //    if (Deleted) return;

        //    if (Client.OwnCells.Count >= ServerConfig.PlayerSplitLimit) return;

        //    if (Size < ServerConfig.PlayerMinSplitSize) return;

        //    FusionC = 0;

        //    CollisionC = ServerConfig.PlayerCollisionTime;

        //    // Get angle
        //    var deltaY = Des.Y - Y;
        //    var deltaX = Des.X - X;
        //    var angle = Math.Atan2(deltaX, deltaY);

        //    // Get starting position
        //    var size = r* ServerConfig.CollisionDeviationConstant;
        //    var startPos = new HKPoint(X + (size * Math.Sin(angle)), Y + (size * Math.Cos(angle)));

        //    var newMass = Size / 2;

        //    //update fathercell mass
        //    Size *= 0.5;

        //    //creat cell
        //    var minion = CellCreator.Generate_Minion(startPos.X, startPos.Y, newMass, Name, color_index, Client, world);
        //    minion.Number = Number;
        //    minion.Client = Client;

        //    minion.MoveTo_ATan_Distance(Client.Parent.Mouce, Math.Min(speed, Cell_Point.CalDistance(Des)), ref minion.transverse, ref minion.longitudinal);
        //}

        public void Pop(World world)
        {
            var rest_split_count = ServerConfig.PlayerSplitLimit - Client.OwnCells.Count;
            var explode = (int)(rest_split_count * 0.75);
            var avg_angle = 360d / explode * Math.PI / 180;
            //均匀扎病毒
            var size = ServerConfig.PopSplit ? Math.Max(Size / explode, ServerConfig.PlayerMinSplitSize * 0.5) : ServerConfig.PlayerMinSplitSize * 0.5;
            for (int j = 0; j < explode; j++)
            {
                //注释掉的是非均匀扎病毒
                //var size = HKRand.Double(Size * 0.1, Size * 0.2);
                //var size = ServerConfig.PlayerMinSplitSize*0.5;
                if (Client.OwnCells.Count < ServerConfig.PlayerSplitLimit)//是否还有剩余空位
                {
                    /*判断细胞质量是否达到最小分裂质量*/
                    if (Size > size * 2 && Size > ServerConfig.PlayerMinSplitSize)
                    {
                        var angle = avg_angle * j;
                        Client.SpaceCount += 1;
                        Size -= size;

                        var minion = CellCreator.Generate_Minion(X, Y, size, Name, color_index, Client, world);
                        CollisionC = 36;
                        FusionC = ServerConfig.PlayerFussionStart;
                        minion.CollisionC = 36;
                        minion.FusionC = ServerConfig.PlayerFussionStart;
                        minion.MoveTo(angle, ServerConfig.VirusExplodeSpeed, ref minion.transverse, ref minion.longitudinal);

                    }

                }
                else break;
            }
        }

        public void Minion_Eject(HKPoint Des, World world)
        {
            if (Size >= ServerConfig.PlayerMinEjectSize)
            {
                if (EjectC >= ServerConfig.PlayerEjectInterval)
                {
                    var size2 = ServerConfig.PlayerEjectSize;
                    var sizeLoss = ServerConfig.PlayerEjectLose;
                    var size1 = Size - sizeLoss;
                    var dx = Des.X - X;
                    var dy = Des.Y - Y;
                    var dl = dx * dx + dy * dy;
                    if (dl is < 1)
                    {
                        dx = 1;
                        dy = 0;
                    }
                    else
                    {
                        dl = Math.Sqrt(dl);
                        dx /= dl;
                        dy /= dl;
                    }

                    // Remove mass from parent cell first
                    Size = size1;

                    // Get starting position
                    var pos = new HKPoint(X + dx * r * 1.05, Y + dy * r * 1.05);
                    var angle = Math.Atan2(dx, dy);
                    if (double.IsNaN(angle)) angle = Math.PI / 2;

                    // Randomize angle
                    angle += (HKRand.Double() * 0.6) - 0.3;

                    // Create cell
                    var eject = new Eject(Des) { Size = size2, X = pos.X, Y = pos.Y, color_index = color_index, Color = Color, DesLocation = Des };
                    eject.Eject_Boost_Distance(Cell_Point, Des, ServerConfig.PlayerEjectSpeed, ref eject.transverse, ref eject.longitudinal);
                    world.BoostCell.Add(eject);
                }
                else
                {
                    EjectC += 1;
                }
            }
        }

        public void ResolveCollision(Minion minion, double dist, World world)
        {
            if (minion is null) return;
            if (IsCollideWith(minion, dist))
                if (Client.Number == minion.Client.Number)
                {
                    CollideWith(minion);
                    world.quadtree.Update(this);
                }
        }

        public bool Equals(Minion other) => other.Id == Id && other.Number == Number;

        public void Dispose()
        {
            Number = 0;
            CollisionC = 0;
            FusionC = 0;
            EjectC = 0;
            Deleted_Clear_Tick = 0;
        }
    }
}
