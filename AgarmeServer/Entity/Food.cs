using AgarmeServer.Client;
using AgarmeServer.Map;
using AgarmeServer.Others;
using AgarmeServer.Zeroer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgarmeServer.Entity
{
    //食物好像没啥好写的唉
    public class Food : Cell,IDisposable
    {
        public List<uint> Sended = new();

        public Food() { }

        public void Tick()
        {
            
        }

        public unsafe void Serialize(IWritableBuffer wb,PlayerClient client,ref uint len)
        {
            //1 + 2 + 2 + 2 + 2 = 9;
            if (Sended.Contains(client.BT) is false)
            {
                if(client.State is ClientState.Connected) 
                {
                    *(byte*)wb.WriteUndefined(1) = Type;
                    *(ushort*)wb.WriteUndefined(2) = (ushort)X;
                    *(ushort*)wb.WriteUndefined(2) = (ushort)Y;
                    *(byte*)wb.WriteUndefined(1) = (byte)R;
                    *(ushort*)wb.WriteUndefined(2) = (ushort)Id;
                    Sended.Add(client.BT);
                }
                else
                {
                    if (client.CheckOutInSight(this))
                    {
                        *(byte*)wb.WriteUndefined(1) = Type;
                        *(ushort*)wb.WriteUndefined(2) = (ushort)X;
                        *(ushort*)wb.WriteUndefined(2) = (ushort)Y;
                        *(byte*)wb.WriteUndefined(1) = (byte)R;
                        *(ushort*)wb.WriteUndefined(2) = (ushort)Id;
                        Sended.Add(client.BT);
                    }
                }
                len++;
            }
        }

        public void Dispose()
        {
            Sended = null;
        }
    }
}
