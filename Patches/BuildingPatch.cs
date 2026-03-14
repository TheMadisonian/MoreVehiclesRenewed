// <copyright file="BuildingPatch.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace MoreVehicles.Patches
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using HarmonyLib;
    using MoreVehicles.Utils;
    using SkyTools.Patching;
    using static MoreVehicles.Utils.Constants;

    /// <summary>
    /// A static class that provides patches for Building vehicle list methods.
    /// The vanilla guards use <c>16384</c> as a cycle-detection threshold for
    /// own-vehicle and guest-vehicle linked lists. With the expanded vehicle
    /// buffer (65536), these guards fire prematurely and cause list traversal
    /// to terminate early, corrupting the linked list and causing simulation freezes.
    /// Patching these guards to use <c>ModdedMaxVehicleCount</c> (65536) preserves
    /// the infinite-loop protection while allowing the full expanded vehicle set.
    /// </summary>
    internal static class BuildingPatch
    {
        /// <summary>Gets the patch for <c>Building.RemoveOwnVehicle</c>.</summary>
        public static IPatch RemoveOwnVehicle { get; } = new Building_RemoveOwnVehicle();

        /// <summary>Gets the patch for <c>Building.IsOwnVehicle</c>.</summary>
        public static IPatch IsOwnVehicle { get; } = new Building_IsOwnVehicle();

        /// <summary>Gets the patch for <c>Building.RemoveGuestVehicle</c>.</summary>
        public static IPatch RemoveGuestVehicle { get; } = new Building_RemoveGuestVehicle();

        /// <summary>Gets the patch for <c>Building.IsGuestVehicle</c>.</summary>
        public static IPatch IsGuestVehicle { get; } = new Building_IsGuestVehicle();

        private sealed class Building_RemoveOwnVehicle : PatchBase
        {
            protected override MethodInfo GetMethod() =>
                typeof(Building).GetMethod(
                    "RemoveOwnVehicle",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(ushort), typeof(Vehicle).MakeByRefType() },
                    new ParameterModifier[0]);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1213", Justification = "Harmony patch")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming Rules", "SA1313", Justification = "Harmony patch")]
            private static IEnumerable<CodeInstruction> Transform(IEnumerable<CodeInstruction> instructions)
                => CodeProcessor.ReplaceOperands(instructions, VanillaMaxVehicleCount, ModdedMaxVehicleCount);
        }

        private sealed class Building_IsOwnVehicle : PatchBase
        {
            protected override MethodInfo GetMethod() =>
                typeof(Building).GetMethod(
                    "IsOwnVehicle",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(ushort) },
                    new ParameterModifier[0]);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1213", Justification = "Harmony patch")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming Rules", "SA1313", Justification = "Harmony patch")]
            private static IEnumerable<CodeInstruction> Transform(IEnumerable<CodeInstruction> instructions)
                => CodeProcessor.ReplaceOperands(instructions, VanillaMaxVehicleCount, ModdedMaxVehicleCount);
        }

        private sealed class Building_RemoveGuestVehicle : PatchBase
        {
            protected override MethodInfo GetMethod() =>
                typeof(Building).GetMethod(
                    "RemoveGuestVehicle",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(ushort), typeof(Vehicle).MakeByRefType() },
                    new ParameterModifier[0]);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1213", Justification = "Harmony patch")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming Rules", "SA1313", Justification = "Harmony patch")]
            private static IEnumerable<CodeInstruction> Transform(IEnumerable<CodeInstruction> instructions)
                => CodeProcessor.ReplaceOperands(instructions, VanillaMaxVehicleCount, ModdedMaxVehicleCount);
        }

        private sealed class Building_IsGuestVehicle : PatchBase
        {
            protected override MethodInfo GetMethod() =>
                typeof(Building).GetMethod(
                    "IsGuestVehicle",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(ushort) },
                    new ParameterModifier[0]);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1213", Justification = "Harmony patch")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming Rules", "SA1313", Justification = "Harmony patch")]
            private static IEnumerable<CodeInstruction> Transform(IEnumerable<CodeInstruction> instructions)
                => CodeProcessor.ReplaceOperands(instructions, VanillaMaxVehicleCount, ModdedMaxVehicleCount);
        }
    }
}
