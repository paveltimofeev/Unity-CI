using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets._CI.Editor
{
    public class CIActions
    {
        public static void Batchmode_PerformBuild()
        {
            Build(BuildTarget.StandaloneWindows);
        }

        public static void Build(BuildTarget buildTarget, string extension = "")
        {
            DateTime start = DateTime.Now;

            string platform = Enum.GetName(typeof(BuildTarget), buildTarget);
            string appVer = CIData.appVer;
            string uniVer = UnityEngine.Application.unityVersion;
            string path = CIData.BuildsFolderName + platform + "-" + appVer + "-" + uniVer + extension;

            LogUtility.log("CI", "Built started for {0} platform. appVer: {1}, uniVer: {2}", platform, appVer, uniVer);

            string err = BuildPipeline.BuildPlayer(CIData.GetScenesForBuild(), path, buildTarget, BuildOptions.None);

            if (!string.IsNullOrEmpty(err))
                LogUtility.error("CI", "Build failed for {0} platform. appVer: {1}, uniVer: {2}. Exception: {3}. Took: {4} sec.", platform, appVer, uniVer, err, (DateTime.Now - start).Seconds);
            else
                LogUtility.log("CI", "Built successfully. Took: {0} sec.", (DateTime.Now - start).Seconds);
        }

        public static void CreatePackage(string name)
        {
            string packagepath = "Assets/Patico";
            string packagename = string.Format("{0}-{1}.unitypackage", name, CIData.appVer);

            LogUtility.log("CI", "Creating '{0}'", packagename);
            AssetDatabase.ExportPackage(packagepath, packagename, ExportPackageOptions.Recurse);
            LogUtility.log("CI", "Package created");
        }

        public static void StartCommand(string exePath, string args)
        {
            try
            {
                LogUtility.log("CI", "Starting command '{0}' with args [{1}]", exePath, args.ToString());
                System.Diagnostics.Process.Start(exePath, args);
            }
            catch (Exception e)
            {
                LogUtility.error("CI", "StartCommand failed for '{0}' with args [{1}]. Exception: {2}", exePath, args.ToString(), e.Message);
            }
        }
    }
}
