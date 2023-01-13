using AgarmeServer.Client;
using AgarmeServer.HKObj;
using AgarmeServer.Map;
using AgarmeServer.Others;
using AgarmeServer.Zeroer;
using HPSocket.Base;
using System;
using System.Collections.Generic;

namespace AgarmeServer.Entity
{
    public class Virus:Cell
    {
        public List<uint> Sended = new List<uint>(3);
        public void Tick(World world)
        {
            if (Deleted) return;

            CountInertia(ref x, ref y, ref transverse, ref longitudinal, ServerConfig.VirusSplitSpeedRate);
            CountInertia(ref x, ref y, ref xd, ref yd, ServerConfig.VirusSplitSpeedRate);//横向纵向衰减

            var r1 = r;

            world.quadtree.Search(Range, (other) => {

                var r2 = other.R;

                var tempDist = Distance(other.Cell_Point);
                if (tempDist > (r1 + r2) * 1.1)
                    return;
                if (other.Id == Id)
                    return;
                if (other.Deleted)
                    return;

                if (other.Type is Constant.TYPE_EJECT)
                {
                    var eject = other as Eject;
                    if (tempDist <= r1 - r2 * ServerConfig.CoverageDegree && Size >= other.Size * ServerConfig.DevourSizeDegree)
                    {
                        Size += other.Size;
                        eject.Deleted = true;
                        /*判断病毒是否可以推动*/
                        if (ServerConfig.IsVirusMoved)
                        {
                            MoveTo(eject.DesLocation, ServerConfig.VirusSplitSpeed, ref transverse, ref longitudinal);
                        }
                        /*病毒达到质量分裂*/
                        if (Size >= ServerConfig.VirusSplitSize)
                        {
                            Split(eject.DesLocation, ServerConfig.VirusSplitSpeed, world);
                        }
                    }
                }
            });
        }

        public unsafe void Serialize(IWritableBuffer wb,PlayerClient client,ref uint Cells_Length)
        {
            if (client.State is ClientState.Connected)
            {
                if (IsStatic())
                {
                    if (!Sended.Contains(client.BT))
                    {
                        wb.Write(Type);
                        wb.Write((byte)1);
                        wb.Write((ushort)x);
                        wb.Write((ushort)y);
                        wb.Write((float)r);
                        wb.Write(Id);
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
                    wb.Write((float)r);
                    wb.Write(Id);
                    Cells_Length++;
                }
            }
            else
            {
                if (client.CheckOutInSight(this) || client.SetSpectateSight(this))
                {
                    if (!IsStatic())
                    {
                        wb.Write(Type);
                        wb.Write((byte)0);
                        wb.Write((ushort)x);
                        wb.Write((ushort)y);
                        wb.Write((float)r);
                        wb.Write(Id);
                        Cells_Length++;
                    }
                    else
                    {
                        if (!Sended.Contains(client.BT))
                        {
                            wb.Write(Type);
                            wb.Write((byte)1);
                            wb.Write((ushort)x);
                            wb.Write((ushort)y);
                            wb.Write((float)r);
                            wb.Write(Id);
                            Cells_Length++;
                        }
                    }
                }
            }
        }

        public void Split(HKPoint Des, double speed, World world)
        {
            var lost_size = Size * 0.5;
            Size -= lost_size;
            Virus virus = new Virus();
            virus.Size = lost_size;
            virus.color_index = this.color_index;
            virus.x = x;
            virus.y = y;
            virus.Type = Constant.TYPE_VIRUS;
            virus.SetID();
            virus.MoveTo(Des, speed, ref virus.transverse, ref virus.longitudinal);
            world.quadtree.Insert(virus);
            world.Cells.Add(virus);

        }

        public bool IsStatic() => Math.Abs(transverse) is <= 0.1 && Math.Abs(longitudinal) is <= 0.1;
    }
}
