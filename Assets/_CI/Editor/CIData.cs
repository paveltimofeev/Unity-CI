using System;
using UnityEditor;
using UnityEngine;

namespace Assets._CI.Editor
{
    public class CIData
    {
        public static readonly string CMD = "cmd";
        public static readonly string EXPLORER = "explorer.exe";

        static readonly string buildsFolderName = "Builds/";
        static readonly string ciScriptsFolder = "CI/";

        public static string BuildsFolderName 
        { 
            get 
            { 
                return buildsFolderName; 
            } 
        }

        public static string BuildsPath
        {
            get
            {
                return GetPathFromRoot(buildsFolderName);
            }
        }

        public static string appVer
        {
            get
            {
                return UnityEngine.Application.version;
            }
        }

        public static string[] GetScenesForBuild()
        {
            return new string[] { "Assets/Patico/Simple Planet Gravity Demo.unity" };
        }

        public static string CleanupCommand
        {
            get
            {
                return string.Format("/c FOR /D %i IN ({0}\\*) DO RD /S /Q \"%i\" && del {0} *.* /Q", buildsFolderName);
            }
        }

        public static string ClearBuildCommand
        {
            get
            {
                return string.Format("/c cd {0} && run && pause", 
                    GetPathFromRoot(ciScriptsFolder));
            }
        }

        public static string ClearBuildWithLatestUnityCommand
        {
            get
            {
                return string.Format("/c cd {0} && run-latest-only && pause", 
                    GetPathFromRoot(ciScriptsFolder));
            }
        }

        public static string GetPathFromRoot(string child)
        {
            string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            return System.IO.Path.GetFullPath(new Uri(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), "../../" + child)).AbsolutePath);
        }
    }

}
