using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace avaness.PistonHeadTools
{
    [ProtoContract]
    public class AddBlockPacket
    {
        [ProtoMember(1)]
        public long gridId;
        [ProtoMember(2)]
        public MyObjectBuilder_PistonTop newTop;

        public const ushort id = 52670;

        public AddBlockPacket()
        { }

        public AddBlockPacket(IMyCubeGrid cubeGrid, MyObjectBuilder_PistonTop newTop)
        {
            gridId = cubeGrid.EntityId;
            this.newTop = newTop;
        }

        public static void Received(ushort id, byte[] data, ulong sender, bool isArrivedFromServer)
        {
            AddBlockPacket temp = MyAPIGateway.Utilities.SerializeFromBinary<AddBlockPacket>(data);
            if (temp != null)
            {
                MyObjectBuilder_PistonTop newTop = temp.newTop;
                IMyCubeGrid grid = MyAPIGateway.Entities.GetEntityById(temp.gridId) as IMyCubeGrid;
                if (newTop != null && grid != null)
                    grid.AddBlock(newTop, false);
            }
        }

        public void SendToOthers()
        {
            if (MyAPIGateway.Session.IsServer)
                MyAPIGateway.Multiplayer.SendMessageToOthers(id, MyAPIGateway.Utilities.SerializeToBinary<AddBlockPacket>(this));
        }
    }
}
