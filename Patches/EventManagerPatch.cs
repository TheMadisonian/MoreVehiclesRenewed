// <copyright file="EventManagerPatch.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace MoreVehicles.Patches
{
    using System;
    using System.Reflection;
    using ColossalFramework;
    using SkyTools.Patching;

    /// <summary>
    /// A static class that provides patches for <see cref="EventManager"/> methods that
    /// iterate the vehicle buffer with <c>ushort</c> loop counters.
    /// </summary>
    /// <remarks>
    /// <c>EventManager.InvalidatePathsOnClosedRoads()</c> uses:
    /// <code>for (ushort num = 1; num &lt; buffer.Length; num = (ushort)(num + 1))</code>
    /// When the vehicle buffer is expanded to 65536 entries, <c>buffer.Length</c> is 65536
    /// but a <c>ushort</c> can only hold 0–65535, so the loop counter wraps from 65535 → 0
    /// and the condition <c>num &lt; 65536</c> is permanently true — infinite loop.
    /// This patch replaces the method via <c>Prefix</c> using an <c>int</c>-based loop.
    /// </remarks>
    internal static class EventManagerPatch
    {
        /// <summary>Gets the patch for <c>EventManager.InvalidatePathsOnClosedRoads</c>.</summary>
        public static IPatch InvalidatePathsOnClosedRoads { get; } = new EventManager_InvalidatePathsOnClosedRoads();

        private sealed class EventManager_InvalidatePathsOnClosedRoads : PatchBase
        {
            protected override MethodInfo GetMethod() =>
                typeof(EventManager).GetMethod(
                    "InvalidatePathsOnClosedRoads",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    new ParameterModifier[0]);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1213", Justification = "Harmony patch")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming Rules", "SA1313", Justification = "Harmony patch")]
            private static bool Prefix()
            {
                var pathManager = Singleton<PathManager>.instance;
                var vehicleManager = Singleton<VehicleManager>.instance;
                Vehicle[] buffer = vehicleManager.m_vehicles.m_buffer;
                NetSegment[] buffer2 = Singleton<NetManager>.instance.m_segments.m_buffer;

                // Use int loop counter — ushort would wrap at 65535 and never reach 65536.
                for (int num = 1; num < buffer.Length; num++)
                {
                    if ((buffer[num].m_flags & Vehicle.Flags.Created) != 0)
                    {
                        int pathPositionIndex = buffer[num].m_pathPositionIndex;
                        pathPositionIndex = ((pathPositionIndex != 255) ? (pathPositionIndex >> 1) : 0);
                        bool flag = false;
                        uint num2 = buffer[num].m_path;
                        int num3 = 0;
                        while (num2 != 0)
                        {
                            int positionCount = pathManager.m_pathUnits.m_buffer[num2].m_positionCount;
                            for (int i = pathPositionIndex; i < positionCount; i++)
                            {
                                ushort segment = pathManager.m_pathUnits.m_buffer[num2].GetPosition(i).m_segment;
                                if (segment > 0 && (buffer2[segment].m_flags2 & NetSegment.Flags2.EventClosed) != 0)
                                {
                                    if (num3 == 0)
                                    {
                                        buffer[num].m_flags2 |= Vehicle.Flags2.EventRoadPass;
                                    }

                                    flag = true;
                                    break;
                                }
                            }

                            if (flag)
                            {
                                VehicleAI vehicleAI = buffer[num].Info.m_vehicleAI;
                                vehicleAI.InvalidatePath((ushort)num, ref buffer[num], (ushort)num, ref buffer[num]);
                                break;
                            }

                            pathPositionIndex = 0;
                            num2 = pathManager.m_pathUnits.m_buffer[num2].m_nextPathUnit;
                            if (++num3 >= 262144)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                    }
                }

                return false; // Skip the original method entirely
            }
        }
    }
}
