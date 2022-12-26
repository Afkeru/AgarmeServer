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
            var val = world.quadtree.GetObjects(Rect).ToArray();
            foreach(var cell in val )
            {
                var r2 = cell.R;
                var tempDist = Distance(cell.Cell_Point);
                if (tempDist > (r1 + r2) * 1.1)
                    continue;
                if (cell.Id == Id)
                    continue;
                if (cell.Deleted)
                    continue;

                if (cell.Type is Constant.TYPE_EJECT)
                {
                    var eject = cell as Eject;
                    if (tempDist <= r1 - r2 * ServerConfig.CoverageDegree && Size >= cell.Size * ServerConfig.DevourSizeDegree)
                    {
                        /*判断病毒是否可以推动*/
                        if (ServerConfig.IsVirusMoved)
                        {
                            MoveTo(eject.DesLocation , ServerConfig.VirusSplitSpeed, ref transverse, ref longitudinal);
                        }
                        /*病毒达到质量分裂*/
                        if (Size >= ServerConfig.VirusSplitSize)
                        {
                            Split(eject.DesLocation, ServerConfig.VirusSplitSpeed,world);
                        }
                        Size += cell.Size;
                        cell.Deleted = true;
                        continue;
                    }
                }
            }
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
            virus.FatherCell = this;
            world.BoostCell.Add(virus);

        }

        public bool IsStatic() => Math.Abs(transverse) is <= 0.1 && Math.Abs(longitudinal) is <= 0.1;
    }
}
