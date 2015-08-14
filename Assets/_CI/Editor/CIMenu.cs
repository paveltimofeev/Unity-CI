using System;
using UnityEditor;
using UnityEngine;

namespace Assets._CI.Editor
{
    public class CIMenu
    {
        [MenuItem("CI/Build StandaloneWindows", false, 1)]
        static void BuildStandaloneWindows()
        {
            CIActions.Build(BuildTarget.StandaloneWindows, ".exe");
        }

        [MenuItem("CI/Build StandaloneWindows64", false, 1)]
        static void BuildStandaloneWindows64()
        {
            CIActions.Build(BuildTarget.StandaloneWindows64, ".exe");
        }

        [MenuItem("CI/Build WebPlayer", false, 1)]
        static void BuildWebPlayer()
        {
            CIActions.Build(BuildTarget.WebPlayer);
        }

        [MenuItem("CI/Build WebGL", false, 1)]
        static void BuildWebGL()
        {
            CIActions.Build(BuildTarget.WebGL);
        }

        [MenuItem("CI/Build android", false, 1)]
        static void BuildAndroid()
        {
            CIActions.Build(BuildTarget.Android);
        }

        [MenuItem("CI/Build All", false, 1)]
        static void BuildAll()
        {
            CIMenu.BuildWebPlayer();
            CIMenu.BuildWebGL();
            CIMenu.BuildStandaloneWindows();
            CIMenu.BuildStandaloneWindows64();
            CIMenu.BuildAndroid();
        }

        [MenuItem("CI/Clear install and build", false, 15)]
        static void RunClearInstallAndBuild()
        {
            CIActions.StartCommand(CIData.CMD, CIData.ClearBuildCommand);
        }

        [MenuItem("CI/Clear install and build with latest Unity", false, 15)]
        static void RunClearInstallAndBuildWithLatestUnity()
        {
            CIActions.StartCommand(CIData.CMD, CIData.ClearBuildWithLatestUnityCommand);
        }

        [MenuItem("CI/Open builds", false, 30)]
        static void RunOpenBuilds()
        {
            CIActions.StartCommand(CIData.EXPLORER, CIData.BuildsPath);
        }

        [MenuItem("CI/Clean all builds", false, 100)]
        static void RunCleanAllBuilds()
        {
            CIActions.StartCommand(CIData.CMD, CIData.CleanupCommand);
        }

        [MenuItem("CI/Create package", false, 115)]
        static void CreatePackage()
        {
            CIActions.CreatePackage("custom-gravitation-kit");
        }


        [MenuItem("CI/Load 3rd-party assets", false, 115)]
        static void LoadThirdPartyAssets()
        {
            Debug.LogWarning("'CI -> Load 3rd-party assets' is not implemented yet");
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            //CIActions.StartCommand(CIData.CMD, CIData.LoadThirdPartyAssetsCommand);
        }

        [MenuItem("CI/Load 3rd-party assets", true)]
        static bool LoadThirdPartyAssetsValidate()
        {
            return false;
        }


        [MenuItem("CI/Merge to cloud-build and push", false, 130)]
        static void MergeToClodBuild()
        {
            Debug.LogWarning("'CI -> Merge to cloud-build and push' is not implemented yet");
        }

        [MenuItem("CI/Merge to cloud-build and push", true)]
        static bool MergeToClodBuildValidate()
        {
            return false;
        }
    }
}
