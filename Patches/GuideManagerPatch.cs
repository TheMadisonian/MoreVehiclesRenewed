// <copyright file="GuideManagerPatch.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace MoreVehicles.Patches
{
    using System.Reflection;
    using SkyTools.Patching;
    using SkyTools.Tools;

    /// <summary>
    /// A static class that provides a null-guard patch for <see cref="GuideManager.ServiceStep"/>.
    /// </summary>
    /// <remarks>
    /// <c>GuideManager.ServiceStep</c> accesses <c>guideController.m_serviceNotUsed</c> and
    /// <c>guideController.m_serviceNeeded</c> without null-checking them first. When a DLC guide
    /// controller is registered for a service but the DLC sprite assets are missing (for example,
    /// <c>IconPolicyCityParkAd</c> not found when Parklife is absent or partially loaded), those
    /// fields can be null, causing a recurring <c>NullReferenceException</c> on every simulation
    /// tick via <c>GuideManager.SimulationStepImpl</c>.
    ///
    /// This Prefix skips the original method call when the guide controller fields are null,
    /// preventing the spam and the simulation error without affecting any other guide behaviour.
    /// </remarks>
    internal static class GuideManagerPatch
    {
        /// <summary>Gets the null-guard patch for <c>GuideManager.ServiceStep</c>.</summary>
        public static IPatch ServiceStep { get; } = new GuideManager_ServiceStep();

        private sealed class GuideManager_ServiceStep : PatchBase
        {
            protected override MethodInfo GetMethod() =>
                typeof(GuideManager).GetMethod(
                    "ServiceStep",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(GuideController), typeof(ItemClass.Service) },
                    new ParameterModifier[0]);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1213", Justification = "Harmony patch")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming Rules", "SA1313", Justification = "Harmony patch")]
            private static bool Prefix(GuideController guideController)
            {
                if (guideController == null)
                {
                    return false;
                }

                if (guideController.m_serviceNotUsed == null || guideController.m_serviceNeeded == null)
                {
                    Log.Warning("'More Vehicles' skipped GuideManager.ServiceStep: guide controller has null service guide fields (missing DLC assets?)");
                    return false;
                }

                return true;
            }
        }
    }
}
