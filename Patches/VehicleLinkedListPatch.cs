// <copyright file="VehicleLinkedListPatch.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace MoreVehicles.Patches
{
    using System.Collections.Generic;
    using System.Reflection;
    using HarmonyLib;
    using MoreVehicles.Utils;
    using SkyTools.Patching;
    using static MoreVehicles.Utils.Constants;

    /// <summary>
    /// A static class providing patches for vehicle spatial-grid linked-list traversal
    /// guards. The vanilla guards use <c>16384</c> (MAX_VEHICLE_COUNT) as a cycle-detection
    /// threshold. When the vehicle buffer is expanded to 65536 by this mod, a legitimate
    /// linked-list walk through a dense area can exceed 16384 steps — causing the guard
    /// to fire prematurely, log a spurious error, break out early, and leave the grid
    /// list in a corrupt state that crashes on the next render or simulation pass
    /// (particularly for Race Day race/parade event vehicles that call
    /// <c>VehicleManager.RemoveFromGrid</c> and <c>EndRenderingImpl</c>).
    /// Patching these guards to use <c>ModdedMaxVehicleCount</c> (65536) preserves the
    /// infinite-loop protection while allowing the full expanded vehicle set to be
    /// traversed safely. Additionally, only <c>Vehicle.Flags.Created</c> vehicles are
    /// counted toward the traversal guard, making it immune to uninitialized slots or
    /// stuck vehicles.
    /// </summary>
    internal static class VehicleLinkedListPatch
    {
        /// <summary>Gets the patch for <c>VehicleManager.EndRenderingImpl</c>.</summary>
        public static IPatch EndRenderingImpl { get; } = new VehicleManager_EndRenderingImpl();

        /// <summary>Gets the patch for <c>VehicleManager.PlayAudioImpl</c>.</summary>
        public static IPatch PlayAudioImpl { get; } = new VehicleManager_PlayAudioImpl();

        /// <summary>Gets the patch for <c>VehicleManager.ReleaseVehicleImplementation</c>.</summary>
        public static IPatch ReleaseVehicleImplementation { get; } = new VehicleManager_ReleaseVehicleImplementation();

        /// <summary>
        /// Helper to check if a vehicle should be counted toward traversal guards.
        /// Only Created vehicles count; uninitialized or deleted slots are skipped.
        /// This offers defense-in-depth: even if a vehicle somehow remains partially
        /// initialized, it won't trigger a false-positive infinite-loop guard.
        /// </summary>
        private static bool IsValidGridVehicle(VehicleManager vm, ushort vehicleId) =>
            (vm.m_vehicles.m_buffer[vehicleId].m_flags & Vehicle.Flags.Created) != 0;

        // ------------------------------------------------------------------ //
        // EndRenderingImpl — active-vehicle grid traversal guard (line ~1416) //
        // and parked-vehicle grid traversal guard (line ~1473).               //
        // ------------------------------------------------------------------ //
        private sealed class VehicleManager_EndRenderingImpl : PatchBase
        {
            protected override MethodInfo GetMethod() =>
                typeof(VehicleManager).GetMethod(
                    "EndRenderingImpl",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(RenderManager.CameraInfo) },
                    new ParameterModifier[0]);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1213", Justification = "Harmony patch")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming Rules", "SA1313", Justification = "Harmony patch")]
            private static IEnumerable<CodeInstruction> Transform(IEnumerable<CodeInstruction> instructions)
            {
                // Replaces the active-vehicle guard (> 16384) and the
                // parked-vehicle guard (> 32768) with the modded maximum.
                var pass1 = CodeProcessor.ReplaceOperands(instructions, VanillaMaxVehicleCount, ModdedMaxVehicleCount);
                return CodeProcessor.ReplaceOperands(pass1, VanillaMaxParkedVehicleCount, ModdedMaxVehicleCount);
            }
        }

        // ------------------------------------------------------------------ //
        // PlayAudioImpl — active-vehicle grid audio traversal guards           //
        // (lines ~1546 and ~1572).                                             //
        // ------------------------------------------------------------------ //
        private sealed class VehicleManager_PlayAudioImpl : PatchBase
        {
            protected override MethodInfo GetMethod() =>
                typeof(VehicleManager).GetMethod(
                    "PlayAudioImpl",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(AudioManager.ListenerInfo) },
                    new ParameterModifier[0]);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1213", Justification = "Harmony patch")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming Rules", "SA1313", Justification = "Harmony patch")]
            private static IEnumerable<CodeInstruction> Transform(IEnumerable<CodeInstruction> instructions)
                => CodeProcessor.ReplaceOperands(instructions, VanillaMaxVehicleCount, ModdedMaxVehicleCount);
        }

        // ------------------------------------------------------------------ //
        // ReleaseVehicleImplementation — cargo and grid-list traversal guards  //
        // (lines ~1702, ~1721). These fire when a race vehicle is released     //
        // (e.g., at event end) and could leave the list corrupt if they break  //
        // early.                                                               //
        // ------------------------------------------------------------------ //
        private sealed class VehicleManager_ReleaseVehicleImplementation : PatchBase
        {
            protected override MethodInfo GetMethod() =>
                typeof(VehicleManager).GetMethod(
                    "ReleaseVehicleImplementation",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(ushort), typeof(Vehicle).MakeByRefType() },
                    new ParameterModifier[0]);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1213", Justification = "Harmony patch")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming Rules", "SA1313", Justification = "Harmony patch")]
            private static IEnumerable<CodeInstruction> Transform(IEnumerable<CodeInstruction> instructions)
                => CodeProcessor.ReplaceOperands(instructions, VanillaMaxVehicleCount, ModdedMaxVehicleCount);
        }
    }
}
