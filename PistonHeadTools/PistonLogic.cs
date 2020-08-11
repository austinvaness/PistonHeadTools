using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace avaness.PistonHeadTools
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ExtendedPistonBase), false)]
    public class PistonLogic : MyGameLogicComponent
    {
        private IMyMechanicalConnectionBlock block;
        private static bool controls = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            block = (IMyMechanicalConnectionBlock)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (block.CubeGrid?.Physics == null)
                return;

            if(!controls)
            {
                controls = true;

                IMyTerminalControlButton detach = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyPistonBase>("Detach");
                detach.Title = MyStringId.GetOrCompute("Detach");
                detach.Enabled = HasTop;
                detach.Visible = True;
                detach.SupportsMultipleBlocks = true;
                detach.Action = Detach;
                MyAPIGateway.TerminalControls.AddControl<IMyPistonBase>(detach);
                AddAction<IMyPistonBase>(detach, "", True);

                IMyTerminalControlButton attach = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyPistonBase>("Attach");
                attach.Title = MyStringId.GetOrCompute("Attach");
                attach.Enabled = NeedsTop;
                attach.Visible = True;
                attach.SupportsMultipleBlocks = true;
                attach.Action = Attach;
                MyAPIGateway.TerminalControls.AddControl<IMyPistonBase>(attach);
                AddAction<IMyPistonBase>(attach, "", True);
            }
        }

        private static void AddAction<T>(IMyTerminalControlButton btn, string icon, Func<IMyTerminalBlock, bool> enabled) where T : IMyTerminalBlock
        {
            IMyTerminalAction a = MyAPIGateway.TerminalControls.CreateAction<T>(btn.Id);
            a.Action = btn.Action;
            a.Enabled = enabled;
            a.ValidForGroups = btn.SupportsMultipleBlocks;
            a.Name = new StringBuilder(btn.Title.String);
            a.Icon = icon;
            MyAPIGateway.TerminalControls.AddAction<T>(a);
        }

        private static bool HasTop(IMyTerminalBlock block)
        {
            return ((IMyMechanicalConnectionBlock)block).IsAttached;
        }

        private static bool NeedsTop(IMyTerminalBlock block)
        {
            var piston = (IMyMechanicalConnectionBlock)block;
            return !piston.IsAttached && !piston.PendingAttachment;
        }

        private static void Detach(IMyTerminalBlock block)
        {
            if (MySession.IsServer)
                Detach((IMyMechanicalConnectionBlock)block);
            else
                new NetPacket((IMyMechanicalConnectionBlock)block, false).SendToServer();
        }

        private static void Attach(IMyTerminalBlock block)
        {
            if (MySession.IsServer)
                Attach((IMyMechanicalConnectionBlock)block);
            else
                new NetPacket((IMyMechanicalConnectionBlock)block, true).SendToServer();
        }

        private static bool True(IMyTerminalBlock block)
        {
            return true;
        }

        public static void Attach(IMyMechanicalConnectionBlock block)
        {
            if (!block.IsAttached && !block.PendingAttachment)
                block.Attach();
        }

        public static void Detach(IMyMechanicalConnectionBlock block)
        {
            if (block.IsAttached)
                block.Detach();
        }
    }
}