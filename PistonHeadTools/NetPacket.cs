using ProtoBuf;
using Sandbox.Game;
using Sandbox.Game.Entities;
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
        public byte mode;

        public const ushort id = 52670;

        public NetPacket()
        { }

        public NetPacket(IMyPistonBase block, byte mode)
        {
            entityId = block.EntityId;
            this.mode = mode;
        }

        public static void Received(byte[] data)
        {
            NetPacket temp = MyAPIGateway.Utilities.SerializeFromBinary<NetPacket>(data);
            if (temp != null)
                temp.Received();
        }

        public void Received()
        {
            IMyPistonBase block = MyAPIGateway.Entities.GetEntityById(entityId) as IMyPistonBase;
            if(block != null)
            {
                if (mode == 0)
                    PistonLogic.Detach(block);
                else if (mode == 1)
                    PistonLogic.Attach(block);
                else
                    PistonLogic.CreateSmallTop(block);
            }
        }

        public void SendToServer()
        {
            if(MySession.Instance.IsServer)
                Received();
            else
                MyAPIGateway.Multiplayer.SendMessageToServer(id, MyAPIGateway.Utilities.SerializeToBinary<NetPacket>(this));
        }
    }
}
