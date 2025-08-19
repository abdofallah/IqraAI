using HarmonyLib;
using System.Reflection;

namespace IqraInfrastructure.Patches
{
    /// <summary>
    /// This class is responsible for applying all our Harmony patches.
    /// Call the Apply() method once at application startup.
    /// </summary>
    public static class IronPatcher
    {
        private const string harmonyId = "com.mycompany.project.ironpdfpatcher";
        private static bool isPatched = false;

        public static void Apply()
        {
            if (isPatched) return;

            var harmony = new Harmony(harmonyId);

            var dummyType = typeof(IronPdf.License);

            // --- Patch IronPDF's Internal License and Analytics Methods ---
            var licenseType = AccessTools.TypeByName("IronPdf.License");
            if (licenseType != null)
            {
                // Patch IsLicensed property
                PatchMethod(harmony, AccessTools.PropertyGetter(licenseType, "IsLicensed"),
                    AccessTools.Method(typeof(IronPdfPatches), nameof(IronPdfPatches.ForceIsLicensed)),
                    "IronPdf.License.IsLicensed");

                // Patch LicenseKey property
                PatchMethod(harmony, AccessTools.PropertyGetter(licenseType, "LicenseKey"),
                    AccessTools.Method(typeof(IronPdfPatches), nameof(IronPdfPatches.ForceLicenseKey)),
                    "IronPdf.License.LicenseKey");

                // Patch orcnjn() to prevent license exceptions
                PatchMethod(harmony, AccessTools.Method(licenseType, "orcnjn"),
                    AccessTools.Method(typeof(IronPdfPatches), nameof(IronPdfPatches.PreventMethodExecution)),
                    "IronPdf.License.orcnjn");

                // NEW: Patch orcnjm() which is likely the analytics/telemetry call
                PatchMethod(harmony, AccessTools.Method(licenseType, "orcnjm"),
                    AccessTools.Method(typeof(IronPdfPatches), nameof(IronPdfPatches.PreventMethodExecution)),
                    "IronPdf.License.orcnjm (Analytics)");
            }
            else
            {
                Console.WriteLine("[Error] IronPatcher: Could not find the type 'IronPdf.License'.");
            }

            isPatched = true;
            Console.WriteLine("\nAll Harmony patches applied.");
        }

        // Helper method to reduce repetitive code
        private static void PatchMethod(Harmony harmony, MethodBase original, MethodInfo patch, string methodName)
        {
            if (original != null)
            {
                harmony.Patch(original, new HarmonyMethod(patch));
                Console.WriteLine($"[OK] IronPatcher: Patched {methodName}");
            }
            else
            {
                Console.WriteLine($"[Error] IronPatcher: Could not find method '{methodName}' to patch.");
            }
        }
    }


    /// <summary>
    /// This class contains the actual patch logic.
    /// </summary>
    public static class IronPdfPatches
    {
        // --- IronPDF Specific Patches ---

        public static bool ForceIsLicensed(ref bool __result)
        {
            __result = true;
            return false; // Skip original
        }

        public static bool ForceLicenseKey(ref string __result)
        {
            __result = "IRONSUITE.TAUSHIF1TEZA.GMAIL.COM.9218-C4C9C0925C-CZRWKKOVBNHWGS-CWR7KUDVDQLI-GCXHX77TEXD5-VJKK7LKZEBJ3-UXXYNFUFTWNI-FSMG77GWWVGP-7W4CTQ-TAZT6SLDBOOLUA-DEPLOYMENT.TRIAL-47I2VM.TRIAL.EXPIRES.09.FEB.2024";
            return false; // Skip original
        }

        /// <summary>
        /// A generic patch that can be used for any void method we want to disable.
        /// It works for both orcnjn (license check) and orcnjm (analytics).
        /// </summary>
        public static bool PreventMethodExecution()
        {
            // Simply return 'false' to skip the original method entirely.
            return false;
        }
    }
}
