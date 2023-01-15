using AgarmeServer.Client;
using AgarmeServer.HKObj;
using AgarmeServer.Map;
using AgarmeServer.Others;
using AgarmeServer.Zeroer;
using HPSocket.Base;
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using Constant = AgarmeServer.Others.Constant;

namespace AgarmeServer.Entity
{
    public class Player:Cell,IDisposable
    {
        public uint BT;
        //以C为结尾的变量都表示计时
        public double CollisionC = 0;//碰撞计时
        public double FusionC = 0;//合体计时
        public double EjectC = 10;//吐球间隔计时
        public double Deleted_Clear_Tick = 0;//死亡清除计时
        private double Biggest_Cell_Eject_Tick = 0;//最大细胞吐球计时
        private double Push_X=0,Push_Y=0;
        private bool Poped = false;//是否炸了病毒
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

            if (Client.State is not ClientState.Playing) {Deleted=true; return; }

            if (Size < 1) return;
            var mouse = Client.Mouce;
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

            Name = Client.Name;

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
            CountComponent(ref x, ref y, ref Push_X, ref Push_Y, 0.75);
            CountInertia(ref x, ref y, ref transverse, ref longitudinal, ServerConfig.PlayerSplitSpeedRate);
            CountInertia(ref x, ref y, ref xd, ref yd, ServerConfig.PlayerMoveSpeedRate);

            //更新最大细胞的位置(Solotrick需要)
            if (ServerConfig.IsSolotrick)
            {
                if (CollisionC is > 0 && Client.Eject && Poped is false)
                {
                    if (Client.CurrentBiggestCell.Id == Id)
                        MoveTo_ATan(Client.Mouce, Speed);
                    else
                        MoveTo_ATan(Client.CurrentBiggestCell.CellPoint(Client.Mouce.Symmetry(Client.CurrentBiggestCell.Cell_Point)), Speed * 1.2);//2.2
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
                if (Client.Eject)
                {
                    if (Client.Macro)
                        if (Size >= ServerConfig.PlayerMinEjectSize)
                            for (var i = 0; i < 25; ++i)
                                if (Size > ServerConfig.PlayerEjectLose)
                                    Player_Eject(Client.Mouce, world);
                                else break;
                        else { }
                    else if (Size >= ServerConfig.PlayerMinEjectSize)
                        if (EjectC >= ServerConfig.PlayerEjectInterval)
                        { Player_Eject(Client.Mouce, world); EjectC = 0; }
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

                if(tempDist > (r+r2)*1.1) return;

                Client.MergeTick++;

                isFussion = FusionC >= FussionConstant && FusionC >= ServerConfig.PlayerFusionTime * ServerConfig.PlayerFusionConstant * Math.Max(Size,64);

                //碰撞
                if (cell.Type is Constant.TYPE_PLAYER)
                {
                    var player = cell as Player;
                    if (ServerConfig.IsSolotrick)//Solotrick部分
                        if (Id == Client.CurrentBiggestCell.Id)
                            if (CollisionC <= ServerConfig.PlayerCollisionTime * 0.15 && (isFussion is false))
                                ResolveCollision(player, tempDist, world);
                            else { }
                        else if (CollisionC <= 0 && player.CollisionC <= 0 && (isFussion is false))
                            ResolveCollision(player, tempDist, world);
                        else { }
                    else if (CollisionC <= 0 && player.CollisionC <= 0 && (isFussion is false))
                        ResolveCollision(player, tempDist, world);

                    //交换大小细胞质量(Solotrick关键代码)
                    if (ServerConfig.IsSolotrick)
                        if(Client.Eject)
                            if (CollisionC > 0 && player.CollisionC > 0 && Id != Client.CurrentBiggestCell.Id && player.Id != Client.CurrentBiggestCell.Id && !Poped)
                                if (ServerConfig.IsSolotrick)
                                    if (Size <= 256 && Client.OwnCells.Count>=32)
                                    {
                                        var m = r + player.r - tempDist;
                                        var M = Size + player.Size;
                                        var aM = player.Size / M;
                                        var bM = Size / M;
                                        if (m is not 0)
                                            if (Size >= player.Size)
                                                MoveTo_ATan_Distance(player.CellPoint(Cell_Point), Math.Min(m, r) * aM * 0.35);
                                            else
                                                player.MoveTo_ATan_Distance(CellPoint(player.Cell_Point), Math.Min(m, r) * bM * 0.2);
                                    }

                }

                if (tempDist <= r1 - r2 * ServerConfig.CoverageDegree)
                {
                    //融合
                    if (cell.Type is Constant.TYPE_PLAYER)
                    {
                        var player = cell as Player;

                        if (Client.BT == player.Client.BT && isFussion && CollisionC is <= 0 && player.CollisionC is <= 0)
                        {
                            FusionC = ServerConfig.PlayerFussionStart;

                            Size += player.Size;
                            player.Deleted = true;
                        }
                    }
                    //吞噬
                    if (Size >= cell.Size * ServerConfig.DevourSizeDegree)
                    {
                        switch (cell.Type)
                        {
                            case Constant.TYPE_PLAYER:
                                {
                                    var player = cell as Player;
                                    //是玩家自己的细胞
                                    if (Client.BT == player.Client.BT)
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
                                    Eat(cell);
                                    cell.Size = 0;
                                    Pop_Edited(world);
                                    //Pop(world);
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
                            case Constant.TYPE_BOT:
                                {
                                    var minion = cell as Minion;
                                    //是自己的侍从
                                    if(Client.MyMinion is not null)
                                    {
                                        if (minion.Number == Client.MyMinion.Number)
                                        {
                                            //......
                                        }
                                    }

                                    if (Client.MyMinion.OwnCells.Count is 1)
                                        Client.MyMinion.OnDead();
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
            if(Client.BT == client.BT)
            {
                *(byte*)wb.WriteUndefined(1) = Type;
                *(float*)wb.WriteUndefined(4) = (float)X;
                *(float*)wb.WriteUndefined(4) = (float)Y;
                *(float*)wb.WriteUndefined(4) = (float)R;
                *(ushort*)wb.WriteUndefined(2) = (ushort)Id;
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
                    *(ushort*)wb.WriteUndefined(2) = (ushort)Id;
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

            FusionC = 0;

            CollisionC = ServerConfig.PlayerCollisionTime;

            var player = CellCreator.Generate_Player(X, Y, lost_size, Name, color_index, Client, world);

            var deviation_vec = player.CalDeviation(Des, 0, r * ServerConfig.CollisionDeviationConstant);

            player.x += deviation_vec.X;

            player.y += deviation_vec.Y;

            player.FusionC = 0;

            player.CollisionC = ServerConfig.PlayerCollisionTime * 0.9;

            player.MoveTo(Des, speed, ref player.transverse, ref player.longitudinal);
        }

        public void Split_Edited(HKPoint Des, double speed, World world)
        {
            if (Deleted) return;

            if (Client.OwnCells.Count >= ServerConfig.PlayerSplitLimit) return;

            if (Size < ServerConfig.PlayerMinSplitSize) return;

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
            var newMass = Size*0.5;

            //update fathercell mass
            Size*=0.5; 

            //creat cell
            var player = CellCreator.Generate_Player(startPos.X, startPos.Y, newMass, Name, color_index, Client, world);
            player.MoveTo_ATan_Distance(Des, size, ref player.Push_X, ref player.Push_Y);
            player.BT = BT;
            player.Client = Client;

            player.MoveTo_ATan_Distance(Des, Math.Max(1.5,speed), ref player.transverse,ref player.longitudinal);
        }

        public void Pop(World world)
        {
            var rest_split_count =ServerConfig.PlayerSplitLimit- Client.OwnCells.Count;
            var explode = ServerConfig.PopSplit ? (int) (rest_split_count*0.85): (int)(rest_split_count * 0.45);
            var avg_angle = 360d / explode * Math.PI / 180;
            //均匀扎病毒
            var size = ServerConfig.PopSplit? Math.Max(Size / explode, ServerConfig.PlayerMinSplitSize*0.5) : ServerConfig.PlayerMinSplitSize * 0.5;
            for (int j = 0; j < explode; j++)
            {
                //注释掉的是非均匀扎病毒
                //var size = HKRand.Double(Size * 0.1, Size * 0.2);
                //var size = ServerConfig.PlayerMinSplitSize*0.5;
                if (Client.OwnCells.Count < ServerConfig.PlayerSplitLimit)//是否还有剩余空位
                {
                    /*判断细胞质量是否达到最小分裂质量*/
                    if (Size > size*2 && Size>ServerConfig.PlayerMinSplitSize)
                    {
                        var angle = avg_angle*j;
                        Client.SpaceCount += 1;
                        Size -= size;

                        var _r = Math.Sqrt(Math.Max((Size - (Math.Min(explode, ServerConfig.PlayerSplitLimit - Client.OwnCells.Count) - 1) * size),32) /Math.PI)*0.4;
                        var start_pos = new HKPoint(X + (_r * Math.Sin(angle)), Y + (_r * Math.Cos(angle)));

                        var player = CellCreator.Generate_Player(start_pos.X, start_pos.Y, size, Name, color_index, Client, world);
                        CollisionC = 35;
                        FusionC = ServerConfig.PlayerFussionStart;
                        player.CollisionC = 10;
                        player.FusionC = ServerConfig.PlayerFussionStart;
                        player.MoveTo_ATan_Distance(angle, _r*0.7, ref player.Push_X, ref player.Push_Y);
                        //player.MoveTo(angle, ServerConfig.VirusExplodeSpeed, ref player.Push_X, ref player.Push_Y);
                        Poped = true;
                        player.Poped = true;
                    }

                }
                else break;
            }
        }

        public void Pop_Edited(World world)
        {
            var splits = DistributeCellMass();
            var rest_split_count = ServerConfig.PlayerSplitLimit - Client.OwnCells.Count;
            var explode = Math.Max(ServerConfig.PopSplit ? splits.Count : (int)Math.Floor(rest_split_count * 0.1),ServerConfig.PlayerSplitLimit-Client.OwnCells.Count);
            var avg_angle = 360d / explode * Math.PI / 180;
            var size_ = ServerConfig.PopSplit ? Size / explode*2 : ServerConfig.PlayerMinSplitSize * 0.5;
            //均匀扎病毒
            var rr_ = r;
            for (int i = 0, l = explode; i < l; i++)
            {
                if (Client.OwnCells.Count < ServerConfig.PlayerSplitLimit)//是否还有剩余空位
                {
                    Client.SpaceCount += 1;
                    //var angle = (double)Misc.RandomDouble() * 2 * Math.PI;
                    var angle = avg_angle * i;
                    CollisionC = 15;
                    FusionC = ServerConfig.PlayerFussionStart;
                    var x_ = x + Math.Sqrt(Math.Max((Size - explode * size_*0.2),1) /Math.PI) * Math.Cos(angle)*0.01;
                    var y_ = y + Math.Sqrt(Math.Max((Size - explode * size_*0.2), 1) / Math.PI) * Math.Sin(angle)*0.01;
                    Size -= size_;
                    var player = CellCreator.Generate_Player(x_, y_, size_, Client.Name, color_index, Client, world);
                    //player.MoveTo(angle, rr_ * 1.2, ref player.Push_X, ref player.Push_Y);
                    player.CollisionC = 20;
                    player.FusionC = ServerConfig.PlayerFussionStart;
                    player.MoveTo(angle, rr_*0.7, ref player.Push_X, ref player.Push_Y);
                    Poped = true;
                    player.Poped = true;
                }
                else
                    break;
            }
        }

        public List<double> DistributeCellMass()
        {
            int i = 0;
            var player = this;
            int cellsLeft = ServerConfig.PlayerSplitLimit - player.Client.OwnCells.Count;
            if (cellsLeft <= 0)
                return new List<double>();
            var splitMin = ServerConfig.PlayerMinSplitSize;
            splitMin = splitMin * splitMin / 100;
            var cellMass = Size;
            if (ServerConfig.PopSplit)
            {
                var amount = (int)Math.Min(Math.Floor(cellMass / splitMin), cellsLeft);
                var perPiece = cellMass / (amount + 1);
                i = 0;
                var doubles = new List<double>();
                while (amount > i)
                {
                    i++;
                    doubles.Add(perPiece);
                }
                return doubles;
            }
            if (cellMass / cellsLeft < splitMin)
            {
                int amount = 2;
                double perPiece;
                while ((perPiece = cellMass / (amount + 1)) >= splitMin && amount * 2 <= cellsLeft)
                    amount *= 2;
                i = 0;
                var doubles = new List<double>();
                while (amount > i)
                {
                    i++;
                    doubles.Add(perPiece);
                }
                return doubles;
            }
            List<double> splits = new();
            var nextMass = cellMass / 2;
            var massLeft = cellMass / 2;
            while (cellsLeft > 0)
            {
                if (nextMass / cellsLeft < splitMin) break;
                while (nextMass >= massLeft && cellsLeft > 1)
                    nextMass /= 2;
                splits.Add(nextMass);
                massLeft -= nextMass;
                cellsLeft--;
            }
            nextMass = massLeft / cellsLeft;
            i = 0;
            while (cellsLeft > i)
            {
                i++;
                splits.Add(nextMass);
            }
            return splits;
        }

        public void Player_Eject(HKPoint Des,World world)
        {
            if (Size >= ServerConfig.PlayerMinEjectSize)
            {
                if (EjectC >= ServerConfig.PlayerEjectInterval)
                {
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
                    Size -= ServerConfig.PlayerEjectLose;

                    // Get starting position
                    var pos = new HKPoint(X+dx*r*1.05,Y+dy*r*1.05);
                    var angle = Math.Atan2(dx, dy);
                    if (double.IsNaN(angle)) angle = Math.PI / 2;

                    // Randomize angle
                    angle += (HKRand.Double() * 0.8) - 0.3;//(HKRand.Double() * 0.6) - 0.3

                    // Create cell
                    var eject = new Eject(Des) { Size = ServerConfig.PlayerEjectSize, X = pos.X,Y = pos.Y, color_index = color_index, Color = Color,DesLocation = Des};
                    if (CollisionC > 0 && Poped is false)
                    {
                        if (Biggest_Cell_Eject_Tick < 3)
                        {
                            eject.Eject_Boost_Distance(Cell_Point, Des, ServerConfig.PlayerEjectSpeed, ref eject.transverse, ref eject.longitudinal);
                            Biggest_Cell_Eject_Tick += 0.4;
                        }
                        else
                        {
                            if (Biggest_Cell_Eject_Tick < 6)
                            {
                                if(FatherCell is not null)
                                {
                                    eject.x = Client.CurrentBiggestCell.x;
                                    eject.y = Client.CurrentBiggestCell.y;
                                }
                                else
                                    eject.Eject_Boost_Distance(Cell_Point, Client.CurrentBiggestCell.Cell_Point, ServerConfig.PlayerEjectSpeed, ref eject.transverse, ref eject.longitudinal);
                                Biggest_Cell_Eject_Tick += 0.4;
                            }
                            else
                                Biggest_Cell_Eject_Tick = 0;
                        }
                    }
                    else
                        eject.Eject_Boost_Distance(Cell_Point, Des, ServerConfig.PlayerEjectSpeed, ref eject.transverse, ref eject.longitudinal);
                    world.BoostCell.Add(eject);
                    //world.Cells.Add(eject);
                    //world.quadtree.Insert(eject);
                }
                else
                    EjectC += 1;
            }
        }

        public void ResolveCollision(Player player,double dist,World world)
        {
            if (IsCollideWith(player, dist))
                if (Client.BT == player.Client.BT)
                {
                    CollideWith(player);
                    world.quadtree.Update(this);
                }
        }

        public void Dispose()
        {
            BT = 0;
            CollisionC = 0;
            FusionC = 0;
            EjectC = 0;
            Deleted_Clear_Tick = 0;
            Biggest_Cell_Eject_Tick = 0;
        }
    }
}
