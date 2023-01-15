using AgarmeServer.Client;
using AgarmeServer.HKObj;
using AgarmeServer.Map;
using AgarmeServer.Others;
using AgarmeServer.Zeroer;
using HPSocket.Base;

namespace AgarmeServer.Entity
{
    public class Eject:Cell,IDisposable
    {
        public List<uint> Sended = new();
        public HKPoint DesLocation = new();/*吐球目标点*/
        public double ClearTimer = 0d;
        public double Invincibility = 1d;

        public Eject(HKPoint desLocation)
        {
            Size = ServerConfig.PlayerEjectSize;
            Type = Constant.TYPE_EJECT;
            SetID();
            DesLocation = desLocation;
            MoveTo(desLocation, Speed, ref transverse, ref longitudinal);
        }

        public void Tick(World world)
        {
            CountInertia(ref x, ref y, ref transverse, ref longitudinal, ServerConfig.PlayerEjectSpeedRate);

            if(Invincibility<=1) 
                Invincibility += 0.3;

            var r1 = R;

            if (ServerConfig.IsClearEject)
            {
                if (ClearTimer >= ServerConfig.EjectClearTime)
                {
                    ClearTimer = 0;
                    Deleted = true;
                    return;
                }
                else
                    ClearTimer += 0.1;
            }

            world.quadtree.Search(Range, (other) => {

                if (ServerConfig.EjectCollision)
                {
                    if (other.Deleted) return;

                    var r2 = other.R;

                    var tempDist = Distance(other.Cell_Point);

                    if (tempDist > (r + r2) * 2) return;

                    if (other.Type is not Constant.TYPE_EJECT) return;

                    if (Id == other.Id) return;

                    if (IsCollideWith(other, tempDist))
                    {
                        CollideWith(other);
                        world.quadtree.Update(this);
                    }
                }
            });
        }

        public unsafe void Serialize(IWritableBuffer wb, PlayerClient client, ref uint Cells_Length)
        {
            //1 + 1 + 2 + 2 + 2 +4 = 12;
            if (client.State is ClientState.Connected)
            {
                if (CheckIsStatic())
                {
                    if (!Sended.Contains(client.BT))
                    {
                        wb.Write(Type);
                        wb.Write((byte)1);
                        wb.Write((float)x);
                        wb.Write((float)y);
                        wb.Write((float)r);
                        wb.Write((ushort)Id);
                        Sended.Add(client.BT);
                        Cells_Length++;
                    }
                }
                else
                {
                    wb.Write(Type);
                    wb.Write((byte)0);
                    wb.Write((ushort)x);
                    wb.Write((ushort)y);
                    wb.Write((ushort)r);
                    wb.Write((ushort)Id);
                    Cells_Length++;
                }
            }
            else
            {
                if (client.CheckOutInSight(this) || client.SetSpectateSight(this))
                {
                    if (!CheckIsStatic())
                    {
                        wb.Write(Type);
                        wb.Write((byte)0);
                        wb.Write((float)x);
                        wb.Write((float)y);
                        wb.Write((float)r);
                        wb.Write((ushort)Id);
                        Cells_Length++;
                    }
                    else
                    {
                        if (!Sended.Contains(client.BT))
                        {
                            wb.Write(Type);
                            wb.Write((byte)1);
                            wb.Write((float)x);
                            wb.Write((float)y);
                            wb.Write((float)r);
                            wb.Write((ushort)Id);
                            Cells_Length++;
                        }
                    }
                }
            }
        }

        public bool CheckIsStatic()
        {
            return Math.Abs(transverse) <= 0.1 && Math.Abs(longitudinal) <= 0.1;
        }

        public void Eject_Boost_Distance(HKPoint p1,HKPoint p2, double move_distance, ref double tran, ref double longi)
        {
            if (p1.Equals(p2))
                return;
            var angle = Math.Atan2((p2.Y - p1.Y), (p2.X - p1.X));//求出夹角的弧度
            if (double.IsNaN(angle)) return;

            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);

            tran += cos * move_distance;
            longi += sin * move_distance;
        }

        public void Dispose()
        {
            Sended = null;
            DesLocation = null;
            ClearTimer = 0;
            Invincibility = 0;
        }

        public bool CanEat() => Invincibility > 1;
    }
}
