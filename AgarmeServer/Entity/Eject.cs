﻿using AgarmeServer.Client;
using AgarmeServer.HKObj;
using AgarmeServer.Map;
using AgarmeServer.Others;
using AgarmeServer.Zeroer;
using HPSocket.Base;

namespace AgarmeServer.Entity
{
    public class Eject:Cell
    {
        public List<uint> Sended = new List<uint>(3);
        public HKPoint DesLocation = new HKPoint();/*吐球目标点*/
        public double ClearTimer = 0d;

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
            if (Deleted) return;

            CountInertia(ref x, ref y, ref transverse, ref longitudinal, ServerConfig.PlayerEjectSpeedRate);

            var r1 = R;
            if (ServerConfig.EjectCollision)
            {
                var val = world.quadtree.GetObjects(Rect).ToArray();

                foreach(var cell in val) 
                {

                    if (cell.Deleted) continue;

                    var r2 = cell.R;

                    var tempDist = Distance(cell.Cell_Point);


                    if (cell.Type is not Constant.TYPE_EJECT) continue;

                    if (Id == cell.Id) continue;

                    if (IsCollideWith(cell, tempDist))
                    {
                        CollideWith(cell);
                    }
                }
            }

            if (ServerConfig.IsClearEject)
            {
                if (ClearTimer >= ServerConfig.EjectClearTime)
                {
                    ClearTimer = 0;
                    Deleted = true;
                    world.quadtree.Remove(this);
                }
                else
                    ClearTimer += 0.1;
            }
            world.quadtree.Move(this);
        }

        public unsafe void Serialize(IWritableBuffer wb, PlayerClient client, ref uint Cells_Length)
        {
            //1 + 2 + 2 + 2 + 2 = 9;
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
                        wb.Write(Id);
                        Sended.Add(client.BT);
                        Cells_Length++;
                    }
                }
                else
                {
                    wb.Write(Type);
                    wb.Write((byte)0);
                    wb.Write((float)x);
                    wb.Write((float)y);
                    wb.Write((float)r);
                    wb.Write(Id);
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
                        wb.Write(Id);
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
                            wb.Write(Id);
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
    }
}
