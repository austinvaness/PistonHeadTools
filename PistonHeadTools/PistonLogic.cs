using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace avaness.PistonHeadTools
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ExtendedPistonBase), false)]
    public class PistonLogic : MyGameLogicComponent
    {
        private IMyPistonBase block;
        private static bool controls = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            block = (IMyPistonBase)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (block.CubeGrid?.Physics == null)
                return;
            AddControls();
        }

        private static void AddControls()
        {
            if (controls)
                return;

            IMyTerminalControlButton detach = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyPistonBase>("Detach");
            detach.Title = MyStringId.GetOrCompute("Detach");
            detach.Enabled = HasTop;
            detach.Visible = True;
            detach.SupportsMultipleBlocks = true;
            detach.Action = DetachClient;
            MyAPIGateway.TerminalControls.AddControl<IMyPistonBase>(detach);
            AddAction<IMyPistonBase>(detach, "", True);

            IMyTerminalControlButton attach = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyPistonBase>("Attach");
            attach.Title = MyStringId.GetOrCompute("Attach");
            attach.Enabled = NeedsTop;
            attach.Visible = True;
            attach.SupportsMultipleBlocks = true;
            attach.Action = AttachClient;
            MyAPIGateway.TerminalControls.AddControl<IMyPistonBase>(attach);
            AddAction<IMyPistonBase>(attach, "", True);

            IMyTerminalControlButton addSmallTop = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyPistonBase>("AddSmallTop");
            addSmallTop.Title = MyStringId.GetOrCompute("Add Small Head");
            addSmallTop.Enabled = (b) => NeedsTop(b) && IsLargePiston(b);
            addSmallTop.Visible = IsLargePiston;
            addSmallTop.SupportsMultipleBlocks = true;
            addSmallTop.Action = CreateSmallTopClient;
            addSmallTop.Tooltip = MyStringId.GetOrCompute("To use small head, piston position must be greater than 0.3");
            MyAPIGateway.TerminalControls.AddControl<IMyPistonBase>(addSmallTop);
            AddAction<IMyPistonBase>(addSmallTop, "", IsLargePiston);

            controls = true;
        }

        private static void DetachClient(IMyTerminalBlock block)
        {
            if (MySession.Instance.IsServer)
                Detach((IMyPistonBase)block);
            else
                new NetPacket((IMyPistonBase)block, 0).SendToServer();
        }

        private static void AttachClient(IMyTerminalBlock block)
        {
            if (MySession.Instance.IsServer)
                Attach((IMyPistonBase)block);
            else
                new NetPacket((IMyPistonBase)block, 1).SendToServer();
        }

        private static void CreateSmallTopClient(IMyTerminalBlock block)
        {
            if (MySession.Instance.IsServer)
                CreateSmallTop((IMyPistonBase)block);
            else
                new NetPacket((IMyPistonBase)block, 2).SendToServer();
        }

        public static void CreateSmallTop(IMyPistonBase block)
        {
            if (block.IsAttached || block.CurrentPosition < 0.3)
                return;

            MatrixD m;
            if(!GetTopMatrix(block, MyCubeSize.Small, out m))
                return;

            List<IMyCubeGrid> temp = new List<IMyCubeGrid>(2);
            string name;
            if (MyAPIGateway.Session.CreativeMode)
                name = "SmallPistonTop";
            else
                name = "SmallPistonTop2";
            MyAPIGateway.PrefabManager.SpawnPrefab(temp, name, m.Translation, m.Forward, m.Up, ownerId: block.OwnerId, callback: () => OnSmallTopCreated(block, temp));
        }

        private static void OnSmallTopCreated(IMyPistonBase block, List<IMyCubeGrid> temp)
        {
            IMyCubeGrid topGrid = temp.FirstOrDefault();
            if (topGrid == null)
                return;

            IMyPistonTop top = topGrid.GetCubeBlock(Vector3I.Zero)?.FatBlock as IMyPistonTop;
            if (top == null)
            {
                MyAPIGateway.Entities.MarkForClose(topGrid);
            }
            else
            {
                block.Attach(top, true);
                MyAPIGateway.Utilities.InvokeOnGameThread(() => FinalizeAttach(block));
            }
        }

        private static bool IsLargePiston(IMyTerminalBlock block)
        {
            return block.CubeGrid != null && block.CubeGrid.GridSizeEnum == MyCubeSize.Large;
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
            return ((IMyPistonBase)block).IsAttached;
        }

        private static bool NeedsTop(IMyTerminalBlock block)
        {
            return !((IMyPistonBase)block).IsAttached;
        }

        private static bool True(IMyTerminalBlock block)
        {
            return true;
        }

        public static void Attach(IMyPistonBase block)
        {
            if (!block.IsAttached)
            {
                IMyPistonTop top = FindTop(block);
                if (top != null)
                    block.Attach(top, true);
                else
                    block.Attach();
                MyAPIGateway.Utilities.InvokeOnGameThread(() => FinalizeAttach(block));
            }
        }

        private static IMyPistonTop FindTop(IMyPistonBase block)
        {
            if (block.CubeGrid == null)
                return null;

            MatrixD topMatrix;
            if (!GetTopMatrix(block, block.CubeGrid.GridSizeEnum, out topMatrix))
                return null;

            Vector3D pos = topMatrix.Translation;
            BoundingSphereD area = new BoundingSphereD(pos, 1);
            foreach(IMyEntity e in MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref area))
            {
                IMyCubeGrid g = e as IMyCubeGrid;
                if(g != null)
                {
                    foreach(IMySlimBlock slim in g.GetBlocksInsideSphere(ref area))
                    {
                        IMyPistonTop top = slim.FatBlock as IMyPistonTop;
                        if (top != null && (top.Base == null || top.Base.Top == null) && TrySetDir(top, block, out top))
                            return top;
                    }
                }
            }
            return null;
        }

        private static bool TrySetDir(IMyPistonTop top, IMyPistonBase piston, out IMyPistonTop result)
        {
            result = top;
            if (((MyCubeGrid)top.CubeGrid).BlocksCount == 1)
            {
                // Top is the only block on the grid, the piston can safely set its orientation.
                return true;
            }

            // Get directions that the top should have.
            Matrix m = WorldToLocal(piston.WorldMatrix, top.CubeGrid.WorldMatrix);
            Base6Directions.Direction forward = Base6Directions.GetClosestDirection(m.Forward);
            Base6Directions.Direction up = Base6Directions.GetClosestDirection(m.Up);

            if(top.Orientation.Up != up)
            {
                // Top must be facing up.
                result = null;
                return false;
            }

            if (top.Orientation.Forward == forward)
            {
                // Top is already in the correct orientation.
                return true;
            }

            // Replace the top with one that is in the correct orientation.
            MyObjectBuilder_PistonTop ob = (MyObjectBuilder_PistonTop)top.GetObjectBuilderCubeBlock(true);
            ob.BlockOrientation = new SerializableBlockOrientation(forward, up);
            top.CubeGrid.RemoveBlock(top.SlimBlock);
            ((MyCubeGrid)top.CubeGrid).SendRemovedBlocks();
            ob.EntityId = 0;
            result = top.CubeGrid.AddBlock(ob, false)?.FatBlock as IMyPistonTop;
            if (result != null)
            {
                ob.EntityId = result.EntityId;
                new AddBlockPacket(top.CubeGrid, ob).SendToOthers();
                return true;
            }
            return false;
        }

        private static void FinalizeAttach(IMyPistonBase block)
        {
            if (!block.IsAttached)
                block.Detach();
        }

        public static void Detach(IMyPistonBase block)
        {
            if (block.IsAttached)
                block.Detach();
        }

        private static MyCubeBlockDefinition GetTopDef(IMyPistonBase block, MyCubeSize size)
        {
            MyPistonBaseDefinition myDef = (MyPistonBaseDefinition)((MyCubeBlock)block).BlockDefinition;
            MyCubeBlockDefinitionGroup defGroup = MyDefinitionManager.Static.TryGetDefinitionGroup(myDef.TopPart);
            return defGroup[size];
        }

        private static bool GetTopMatrix(IMyPistonBase block, MyCubeSize topSize, out MatrixD matrix, MyCubeBlockDefinition def = null)
        {
            MyEntitySubpart subpart1;
            MyEntitySubpart subpart2;
            MyEntitySubpart subpart3;
            if (block.Closed || !block.TryGetSubpart("PistonSubpart1", out subpart1) || (!subpart1.Subparts.TryGetValue("PistonSubpart2", out subpart2) || !subpart2.Subparts.TryGetValue("PistonSubpart3", out subpart3)))
            {
                matrix = MatrixD.Identity;
                return false;
            }

            IMyModelDummy subpart;
            if (!TryGetDummy("TopBlock", subpart3.Model, out subpart))
            {
                matrix = MatrixD.Identity;
                return false;
            }

            MatrixD m = block.WorldMatrix;
            m.Translation = Vector3D.Transform(subpart.Matrix.Translation, subpart3.WorldMatrix);

            if (block.CubeGrid != null && topSize == block.CubeGrid.GridSizeEnum)
            {
                if (def == null)
                    def = GetTopDef(block, topSize);
                if (def.Center != Vector3.Zero)
                    m.Translation = Vector3D.Transform(-def.Center * MyDefinitionManager.Static.GetCubeSize(topSize), m);
            }
            else if(topSize == MyCubeSize.Small)
            {
                m.Translation += m.Up * 0.5;
            }

            matrix = m;
            return true;
        }

        private static bool TryGetDummy(string name, IMyModel model, out IMyModelDummy dummy)
        {
            Dictionary<string, IMyModelDummy> dummies = new Dictionary<string, IMyModelDummy>();
            model.GetDummies(dummies);
            return dummies.TryGetValue(name, out dummy);
        }

        private static MatrixD WorldToLocal(MatrixD m, MatrixD refMatrix)
        {
            return m * MatrixD.Normalize(MatrixD.Invert(refMatrix));
        }
    }
}