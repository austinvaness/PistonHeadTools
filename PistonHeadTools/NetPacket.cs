using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace avaness.PistonHeadTools
{
    [ProtoContract]
    public class NetPacket
    {
        [ProtoMember(1)]
        public long entityId;
        [ProtoMember(2)]
        public bool attach;

        public const ushort id = 52670;

        public NetPacket()
        { }

        public NetPacket(IMyMechanicalConnectionBlock block, bool attach)
        {
            entityId = block.EntityId;
            this.attach = attach;
        }

        public static void Received(byte[] data)
        {
            NetPacket temp = MyAPIGateway.Utilities.SerializeFromBinary<NetPacket>(data);
            if (temp != null)
                temp.Received();
        }

        public void Received()
        {
            IMyMechanicalConnectionBlock block = MyAPIGateway.Entities.GetEntityById(entityId) as IMyMechanicalConnectionBlock;
            if(block != null)
            {
                if (attach)
                    PistonLogic.Attach(block);
                else
                    PistonLogic.Detach(block);
            }
        }

        public void SendToServer()
        {
            if(MySession.IsServer)
                Received();
            else
                MyAPIGateway.Multiplayer.SendMessageToServer(id, MyAPIGateway.Utilities.SerializeToBinary<NetPacket>(this));
        }
    }
}
