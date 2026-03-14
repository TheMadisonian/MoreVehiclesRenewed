// <copyright file="CinematicCameraControllerPatch.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace MoreVehicles.Patches
{
    using System;
    using System.Reflection;
    using ColossalFramework;
    using HarmonyLib;
    using MoreVehicles.Utils;
    using SkyTools.Patching;
    using UnityEngine;
    using static MoreVehicles.Utils.Constants;

    /// <summary>
    /// A static class that provides patches for <c>CinematicCameraController</c> vehicle-search methods.
    /// </summary>
    /// <remarks>
    /// The vanilla methods use <c>ushort</c> loop counters with a bound of <c>16384</c>.
    /// A naive IL transpiler that replaces <c>16384</c> with <c>65536</c> produces an
    /// infinite loop: a <c>ushort</c> can never equal <c>65536</c> (max value 65535 wraps
    /// to 0 on increment), so the loop condition is permanently true.
    /// These patches replace the entire methods via <c>Prefix</c> using <c>int</c>-based
    /// loops that correctly cover all <c>ModdedMaxVehicleCount</c> (65536) vehicle slots.
    /// </remarks>
    internal static class CinematicCameraControllerPatch
    {
        /// <summary>Gets the patch for the <c>GetRandomVehicle</c> method.</summary>
        public static IPatch GetRandomVehicle { get; } = new CinematicCameraController_GetRandomVehicle();

        /// <summary>Gets the patch for the <c>GetVehicleWithName</c> method.</summary>
        public static IPatch GetVehicleWithName { get; } = new CinematicCameraController_GetVehicleWithName();

        /// <summary>Gets the patch for the <c>GetNearestVehicle</c> method.</summary>
        public static IPatch GetNearestVehicle { get; } = new CinematicCameraController_GetNearestVehicle();

        // Cached delegate for the private static IsInsideCityLimits(Vector3) method.
        private static readonly Func<Vector3, bool> IsInsideCityLimits =
            (Func<Vector3, bool>)Delegate.CreateDelegate(
                typeof(Func<Vector3, bool>),
                typeof(CinematicCameraController).GetMethod(
                    "IsInsideCityLimits",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(Vector3) },
                    null));

        // ------------------------------------------------------------------ //
        // GetRandomVehicle                                                    //
        // ------------------------------------------------------------------ //
        private sealed class CinematicCameraController_GetRandomVehicle : PatchBase
        {
            protected override MethodInfo GetMethod() =>
                typeof(CinematicCameraController).GetMethod(
                    nameof(CinematicCameraController.GetRandomVehicle),
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Level) },
                    new ParameterModifier[0]);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1213", Justification = "Harmony patch")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming Rules", "SA1313", Justification = "Harmony patch")]
            private static bool Prefix(
                ItemClass.Service service,
                ItemClass.SubService subService,
                ItemClass.Level level,
                ref ushort __result)
            {
                // Start from a random offset and scan the full expanded buffer using
                // an int counter (not ushort) to avoid the wrap-around infinite loop.
                int start = Singleton<SimulationManager>.instance.m_randomizer.Int32(1, ModdedMaxVehicleCount - 1);
                var buffer = VehicleManager.instance.m_vehicles.m_buffer;
                int num = start;

                for (int i = 0; i < ModdedMaxVehicleCount - 1; i++)
                {
                    if (++num >= ModdedMaxVehicleCount)
                    {
                        num = 1;
                    }

                    if (buffer[num].m_flags != 0)
                    {
                        VehicleInfo info = buffer[num].Info;
                        if ((service == ItemClass.Service.None || info.GetService() == service)
                            && (subService == ItemClass.SubService.None || info.GetSubService() == subService)
                            && (level == ItemClass.Level.None || info.GetClassLevel() == level))
                        {
                            InstanceID id = default;
                            id.Vehicle = (ushort)num;
                            if (InstanceManager.GetPosition(id, out Vector3 position, out _, out _)
                                && IsInsideCityLimits(position))
                            {
                                __result = (ushort)num;
                                return false;
                            }
                        }
                    }
                }

                __result = 0;
                return false;
            }
        }

        // ------------------------------------------------------------------ //
        // GetVehicleWithName                                                  //
        // ------------------------------------------------------------------ //
        private sealed class CinematicCameraController_GetVehicleWithName : PatchBase
        {
            protected override MethodInfo GetMethod() =>
                typeof(CinematicCameraController).GetMethod(
                    nameof(CinematicCameraController.GetVehicleWithName),
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(string) },
                    new ParameterModifier[0]);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1213", Justification = "Harmony patch")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming Rules", "SA1313", Justification = "Harmony patch")]
            private static bool Prefix(string name, ref ushort __result)
            {
                var buffer = VehicleManager.instance.m_vehicles.m_buffer;
                for (int i = 1; i < ModdedMaxVehicleCount; i++)
                {
                    if ((buffer[i].m_flags & Vehicle.Flags.CustomName) != 0)
                    {
                        InstanceID id = default;
                        id.Vehicle = (ushort)i;
                        if (Singleton<InstanceManager>.instance.GetName(id)
                            .Equals(name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            __result = (ushort)i;
                            return false;
                        }
                    }
                }

                __result = 0;
                return false;
            }
        }

        // ------------------------------------------------------------------ //
        // GetNearestVehicle                                                   //
        // ------------------------------------------------------------------ //
        private sealed class CinematicCameraController_GetNearestVehicle : PatchBase
        {
            protected override MethodInfo GetMethod() =>
                typeof(CinematicCameraController).GetMethod(
                    nameof(CinematicCameraController.GetNearestVehicle),
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(Vector3) },
                    new ParameterModifier[0]);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1213", Justification = "Harmony patch")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming Rules", "SA1313", Justification = "Harmony patch")]
            private static bool Prefix(Vector3 position, ref ushort __result)
            {
                float minSqrDist = float.MaxValue;
                ushort result = 0;
                var buffer = VehicleManager.instance.m_vehicles.m_buffer;

                for (int i = 1; i < ModdedMaxVehicleCount; i++)
                {
                    if ((buffer[i].m_flags & (Vehicle.Flags.Created | Vehicle.Flags.Deleted)) != Vehicle.Flags.Created)
                    {
                        continue;
                    }

                    InstanceID id = default;
                    id.Vehicle = (ushort)i;
                    if (InstanceManager.GetPosition(id, out Vector3 vehiclePos, out _, out _))
                    {
                        float sqrDist = (position - vehiclePos).sqrMagnitude;
                        if (sqrDist < minSqrDist)
                        {
                            minSqrDist = sqrDist;
                            result = (ushort)i;
                        }
                    }
                }

                __result = result;
                return false;
            }
        }
    }
}
