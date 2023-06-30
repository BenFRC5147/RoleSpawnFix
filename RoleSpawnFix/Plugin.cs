using HarmonyLib;
using Interactables.Interobjects;
using PlayerRoles.FirstPersonControl;
using PlayerRoles;
using PlayerRoles.FirstPersonControl.Spawnpoints;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection.Emit;
using GameObjectPools;
using System.Reflection;
using NorthwoodLib.Pools;
using static HarmonyLib.AccessTools;
using Mirror;
using Unity.Profiling;
using PluginAPI.Core;
using MEC;
using RoundRestarting;

namespace RoleSpawnFix
{
    public class Plugin
    {
        public static Harmony Harmony { get; private set; }
        [PluginEntryPoint("RoleSpawnFix", "1.0.0", "Another NW moment fixed", "Steven4547466")]
        void LoadPlugin()
        {
            Log.Info("--------LOADED--------");

            PlayerRoleManager.OnRoleChanged += delegate (ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
            {
                if (!NetworkServer.active)
                {
                    Log.Error("NetworkServer.active == false");
                    return;
                }
                Vector3 position;
                float currentHorizontal;
                if (!newRole.RoleTypeId.GetRandomSpawnPoint(out position, out currentHorizontal))
                {
                    if (newRole.RoleTypeId != RoleTypeId.Spectator && newRole.RoleTypeId != RoleTypeId.None && newRole.RoleTypeId != RoleTypeId.Scp0492)
                        Log.Error($"Unable to get random spawn point for role: {newRole.RoleTypeId}. GetRandomSpawnPoint() returned false.");
                    return;
                }
                if (!newRole.ServerSpawnFlags.HasFlag(RoleSpawnFlags.UseSpawnpoint))
                {
                    Log.Error("ServerSpawnFlags does not contain UseSpawnpoint");
                    return;
                }
                Player player = Player.Get(hub);
                if (player == null)
                {
                    Log.Error("Player == null");
                    return;
                }
                hub.transform.position = position;
                if (newRole is IFpcRole fpcRole)
                    fpcRole.FpcModule.MouseLook.CurrentHorizontal = currentHorizontal;
                if (!hub.TryOverridePosition(position, Vector3.zero))
                {
                    Log.Error("TryOverridePosition returned false");
                }

                Timing.CallDelayed(Time.fixedDeltaTime * 5f, () =>
                {
                    player.Rotation = Vector3.up * currentHorizontal;
                });
            };
        }
    }

    public static class Extensions
    {
        internal static bool GetRandomSpawnPoint(this RoleTypeId roleType, out Vector3 pos, out float rot)
        {
            pos = Vector3.zero;
            rot = 0f;
            if (!PlayerRoleLoader.TryGetRoleTemplate(roleType, out PlayerRoleBase roleBase))
            {
                if (roleType != RoleTypeId.Spectator && roleType != RoleTypeId.None)
                    Log.Error("Role template does not exist");
                return false;
            }

            if (!(roleBase is IFpcRole fpc))
            {
                if (roleType != RoleTypeId.Spectator && roleType != RoleTypeId.None)
                    Log.Error("RoleBase is not IFpcRole");
                return false;
            }

            ISpawnpointHandler spawn = fpc.SpawnpointHandler;
            if (spawn is null)
            {
                if (roleType != RoleTypeId.Spectator && roleType != RoleTypeId.None && roleType != RoleTypeId.Scp0492)
                    Log.Error("Spawn is null");
                return false;
            }

            return spawn.TryGetSpawnpoint(out pos, out rot);
        }
    }
}