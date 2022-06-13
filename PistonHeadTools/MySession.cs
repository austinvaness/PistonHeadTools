using System.Collections.Generic;
using System;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;

namespace avaness.PistonHeadTools
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MySession : MySessionComponentBase
    {
        private Random rand = new Random();

        public static MySession Instance;
        public bool IsServer => MyAPIGateway.Session.IsServer || MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;


        public override void BeforeStart()
        {
            Instance = this;

            MyAPIGateway.TerminalControls.CustomControlGetter += TerminalControls_CustomControlGetter;
            if (IsServer)
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetPacket.id, NetPacket.Received);
            else
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(AddBlockPacket.id, AddBlockPacket.Received);
        }

        protected override void UnloadData()
        {
            Instance = null;

            MyAPIGateway.TerminalControls.CustomControlGetter -= TerminalControls_CustomControlGetter;
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetPacket.id, NetPacket.Received);
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(AddBlockPacket.id, AddBlockPacket.Received);
        }

        private static void TerminalControls_CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (!(block is IMyPistonBase))
                return;

            IMyTerminalControl detach = null;
            IMyTerminalControl attach = null;
            IMyTerminalControl smallTop = null;
            int move = 0;
            for (int i = controls.Count - 1; i >= 0; i--)
            {
                IMyTerminalControl c = controls[i];
                if (move >= 2 && c.Id == "Reverse")
                {
                    if(smallTop != null)
                    {
                        controls[i] = smallTop;
                        i++;
                        controls[i] = c;
                    }
                    if (attach != null && detach != null)
                    {
                        controls[i + 1] = detach;
                        controls[i + 2] = attach;
                    }
                    break;
                }

                if (move > 0)
                    controls[i + move] = c;

                if (move < 3)
                {
                    if (c.Id == "Attach")
                    {
                        attach = c;
                        move++;
                    }
                    else if (c.Id == "Detach")
                    {
                        detach = c;
                        move++;
                    }
                    else if(c.Id == "AddSmallTop")
                    {
                        smallTop = c;
                        move++;
                    }
                }
            }
        }

        private long RandomLong()
        {
            byte[] bytes = new byte[8];
            rand.NextBytes(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        public bool RandomEntityId(out long id)
        {
            for (int i = 0; i < 100; i++)
            {
                id = RandomLong();
                if (id != 0 && !MyAPIGateway.Entities.EntityExists(id))
                    return true;
            }
            id = 0;
            return false;
        }
    }
}