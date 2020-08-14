using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.Components;

namespace avaness.PistonHeadTools
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MySession : MySessionComponentBase
    {
        public static bool IsServer => MyAPIGateway.Session.IsServer || MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;

        public override void BeforeStart()
        {
            MyAPIGateway.TerminalControls.CustomControlGetter += TerminalControls_CustomControlGetter;
            if (IsServer)
                MyAPIGateway.Multiplayer.RegisterMessageHandler(NetPacket.id, NetPacket.Received);
        }

        protected override void UnloadData()
        {
            MyAPIGateway.TerminalControls.CustomControlGetter -= TerminalControls_CustomControlGetter;
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(NetPacket.id, NetPacket.Received);
        }

        private static void TerminalControls_CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (!(block is IMyPistonBase))
                return;

            IMyTerminalControl detach = null;
            IMyTerminalControl attach = null;
            int move = 0;
            for (int i = controls.Count - 1; i >= 0; i--)
            {
                IMyTerminalControl c = controls[i];
                if (move == 2 && c.Id == "Reverse")
                {
                    if (attach != null && detach != null)
                    {
                        controls[i + 1] = detach;
                        controls[i + 2] = attach;
                    }
                    break;
                }

                if (move > 0)
                    controls[i + move] = c;

                if (move < 2)
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
                }
            }
        }
    }
}