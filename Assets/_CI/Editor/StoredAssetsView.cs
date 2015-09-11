using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Specialized;

namespace Patico.AssetManager
{
    class StoredAssetsView : EditorWindow
    {
        private AssetManager assetManager = new AssetManager();
        
        private readonly GUIContent m_GUIRefreshList = new GUIContent("Refresh list", "Refresh list of downloaded assets");
        private readonly GUIContent m_GUIStoreAssets = new GUIContent("Save .assets", "Store selected assets to .assets list");
        private readonly GUIContent m_GUIImportAssets = new GUIContent("Import .assets", "Import assets from .assets list");
        private readonly GUIContent m_GUIInstallSelected = new GUIContent("Install selected", "Install selected packages");

        private string assetsFilePath = string.Empty;
        private string m_searchFilter = "";
        private Hashtable publisher_foldout_table = new Hashtable();
        private IList<StoredAsset> publishers = new List<StoredAsset>();
        private List<StoredAsset> storedAssets = new List<StoredAsset>();
        private Vector2 m_Scroll = Vector2.zero;
        private Group m_Group = Group.GroupByPublisher;

        
        public void OnEnable()
        {
            titleContent = new GUIContent("Assets Manager");
            assetsFilePath = Path.Combine(Environment.CurrentDirectory, ".assets");

            storedAssets = assetManager.RefreshList();
        }

        public void OnDestroy()
        {

        }
        
        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            
            DrawToolbar();

            DrawAssetsList(storedAssets);

            EditorGUILayout.EndVertical();
        }

   
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            DrawToolbarButton(m_GUIRefreshList, () =>
            {
                storedAssets = assetManager.RefreshList();
                publishers = assetManager.CreatePublisherList(storedAssets);

                publisher_foldout_table = new Hashtable();
                foreach (var item in publishers)
                {
                    publisher_foldout_table[item.publisher] = false;
                }
            });

            m_Group = (Group)EditorGUILayout.EnumPopup(m_Group, EditorStyles.toolbarDropDown);

            if (m_Group == Group.All)
            {
                m_searchFilter = EditorGUILayout.TextField(m_searchFilter, EditorStyles.toolbarTextField);
            }

            GUILayout.FlexibleSpace();

            DrawToolbarButton(m_GUIStoreAssets, () =>
            {
                var selected = storedAssets.Where(x => x.selected);
                assetManager.SaveAssets(selected.ToList(), assetsFilePath);
            });

            DrawToolbarButton(m_GUIImportAssets, () =>
            {
                IList<StoredAsset> imported = assetManager.LoadAssets(assetsFilePath).Dependencies;
                assetManager.SelectAssets(storedAssets, imported);
            });

            DrawToolbarButton(m_GUIInstallSelected, () =>
            {
                assetManager.InstallPackages(
                    storedAssets.Where(x => x.selected).ToList());
            });
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbarButton(GUIContent content, Action action)
        {
            if (GUILayout.Button(content, EditorStyles.toolbarButton))
                action();
        }

        private void DrawAssetsList(IList<StoredAsset> assets)
        {
            EditorGUILayout.BeginVertical();
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(2000));

            if (assets != null)
            {
                if (m_Group == Group.GroupByPublisher)
                {
                    foreach (StoredAsset publisher in publishers)
                    {
                        if (publisher_foldout_table.ContainsKey(publisher.publisher))
                        {
                            bool foldout = EditorGUILayout.Foldout((bool)publisher_foldout_table[publisher.publisher], 
                                publisher.publisher);

                            publisher_foldout_table[publisher.publisher] = foldout;

                            if (foldout)
                            {
                                var publisherAssets = assets.Where(x => x.publisher == publisher.publisher).ToList();

                                foreach (StoredAsset asset in publisherAssets)
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Space(10);
                                    asset.selected = DrawAssetToggle(asset, DrawFlag.name | DrawFlag.version | DrawFlag.size);
                                    GUILayout.EndHorizontal();
                                }
                            }
                        }
                    }
                }

                if (m_Group == Group.All)
                {
                    foreach (StoredAsset asset in assets)
                    {
                        if (IsFiltered(asset.name) || IsFiltered(asset.publisher))
                            asset.selected = DrawAssetToggle(asset, DrawFlag.publisher | DrawFlag.name | DrawFlag.version | DrawFlag.size);
                    }
                }

                if (m_Group == Group.SelectedOnly)
                {
                    foreach (StoredAsset asset in assets)
                    {
                        if (asset.selected)
                            asset.selected = DrawAssetToggle(asset, DrawFlag.category | DrawFlag.name | DrawFlag.size);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private bool DrawAssetToggle(StoredAsset asset, DrawFlag draw)
        {
            StringBuilder sb = new StringBuilder();
            
            if ((draw & DrawFlag.publisher) == DrawFlag.publisher) 
                sb.AppendFormat("{0} | ", asset.publisher);

            if ((draw & DrawFlag.category) == DrawFlag.category)
                sb.AppendFormat("{0} / ", asset.category);

            if ((draw & DrawFlag.name) == DrawFlag.name) 
                sb.AppendFormat("{0} ", asset.name);
            
            if ((draw & DrawFlag.version) == DrawFlag.version) 
                sb.AppendFormat("[{0}] ", asset.version);

            if ((draw & DrawFlag.size) == DrawFlag.size) 
                sb.AppendFormat("({0} Mb)", asset.size);

            return GUILayout.Toggle(asset.selected, sb.ToString());
        }


        private bool IsFiltered(string value)
        {
            if (m_searchFilter == "" || m_searchFilter == null)
                return true;

            return value.ToUpperInvariant().Contains(m_searchFilter.ToUpperInvariant());
        }

        [MenuItem("Window/Assets Manager", false, 116)]
        private static void LoadThirdPartyAssets()
        {
            GetWindow(typeof(StoredAssetsView)).Show();
        }
    }

    class AssetManager
    {
        private const string k_appUnityFolder = "Unity";
        private const string k_assetsFolderPattern = "Asset Store*";
        private const string k_assetsFilePattern = "*.unitypackage";

        public List<StoredAsset> RefreshList()
        {
            // IF WIN
            string appdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), k_appUnityFolder);

            return RefreshList(appdata);
        }

        public List<StoredAsset> RefreshList(string sourceFolder)
        {
            List<StoredAsset> storedAssets = new List<StoredAsset>();

            string[] folders = Directory.GetDirectories(sourceFolder, k_assetsFolderPattern);

            foreach (var folder in folders)
            {
                foreach (string package in Directory.GetFiles(folder, k_assetsFilePattern, SearchOption.AllDirectories))
                {
                    FileInfo packageInfo = new FileInfo(package);
                    storedAssets.Add(
                        new StoredAsset()
                        {
                            path = package,
                            name = Path.GetFileNameWithoutExtension(package),
                            publisher = packageInfo.Directory.Parent.Name,
                            category = packageInfo.Directory.Name,
                            version = Path.GetFileNameWithoutExtension(folder),
                            size = packageInfo.Length / 1024 / 1024
                        });
                }
            }

            return storedAssets;
        }

        public void SaveAssets(IList<StoredAsset> assets, string filePath)
        {
            SaveAssets(new AseetsConfig(){ Dependencies = assets }, filePath);
        }
        public void SaveAssets(AseetsConfig config, string filePath)
        {
            StringBuilder content = new StringBuilder();

            foreach (AssetSource source in config.Sources)
            {
                /// ~MyPackages://C:\Users\Me\MyUnityPackages\
                content.AppendFormat("~{0}://{1}\r\n", source.Name, source.Location);
            }

            foreach (StoredAsset asset in config.Dependencies)
            {
                /// DefaultUnity://patico/scriptingphysics/CustomGravitationsKit
                content.AppendFormat("{0}://{1}/{2}/{3}\r\n", asset.Storage, asset.publisher, asset.category, asset.name);
            }

            File.WriteAllText(filePath, content.ToString());
        }

        public AseetsConfig LoadAssets(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            AseetsConfig config = new AseetsConfig();
            foreach (string data in lines)
            {
                char first = data.Length > 0 ? data[0] : '#';
                switch (first)
                {
                    case '#': break; /// comment line
                    case '~':
                        IParser parser = new AssetSourceParser();
                        parser.Parse(data);
                        config.Sources.Add(new AssetSource() { Name = parser.Parsed[0], Location = parser.Parsed[1] });
                        break;

                    default:
                        IParser assetparser = new StoredAssetParser();
                        assetparser.Parse(data);

                        config.Dependencies.Add(new StoredAsset()
                        {
                            Storage = assetparser.Parsed[0],
                            publisher = assetparser.Parsed[1],
                            category = assetparser.Parsed[2],
                            name = assetparser.Parsed[3]
                        });
                        break;
                }
            }

            return config;
        }

        public void InstallPackages(IList<StoredAsset> assets)
        {
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    AssetDatabase.ImportPackage(asset.path, false);
                }
            }
        }

        public void SelectAssets(List<StoredAsset> storedAssets, IList<StoredAsset> imported)
        {
            storedAssets.ForEach(a => a.selected = false);
            foreach (var item in imported)
            {
                storedAssets.Find(
                    x =>
                        x.publisher == item.publisher &&
                        x.category == item.category &&
                        x.name == item.name

                    ).selected = true;
            }
        }

        public IList<StoredAsset> CreatePublisherList(IList<StoredAsset> assets)
        {
            return assets.GroupBy(x => x.publisher).Select(x => x.First()).ToList();
        }        
    }

    interface IParser
    {
        string[] Parsed { get; }
        void Parse(string text);
    }
    class AssetSourceParser : IParser
    {
        public string[] Parsed { get; private set; }
        public void Parse(string text)
        {
            Parsed = text.Split(new string[] { "~", "://" }, StringSplitOptions.None);
        }
    }
    class StoredAssetParser : IParser
    {
        const string _storageSeparator = "://";
        const char _pathSeparator = '/';

        /// <summary>
        /// First element is Storage, others is path elements
        /// </summary>
        public string[] Parsed { get; private set; }
        public void Parse(string text)
        {
            StringCollection values = new StringCollection();
            
            string[] raw = text.Split(new string[] { _storageSeparator }, StringSplitOptions.None);

            // storage
            values.Add(raw.Length > 1 ? raw[0]: string.Empty);
            
            // location
            values.AddRange(raw[raw.Length - 1].Split(_pathSeparator));

            Parsed = new string[values.Count];
            values.CopyTo(Parsed, 0);
        }
    }

    // POCOs
    class AseetsConfig
    {
        public IList<AssetSource> Sources { get; set; }
        public IList<StoredAsset> Dependencies { get; set; }

        public AseetsConfig()
        {
            Sources = new List<AssetSource>();
            Dependencies = new List<StoredAsset>();
        }
    }
    class AssetSource
    {
        public string Name { get; set; }
        public string Location { get; set; }
    }
    class StoredAsset
    {
        const string _defaultStorage = "DefaultUnity";

        public string Storage { get; set; }
        public string name { get; set; }
        public string publisher { get; set; }
        /// <summary>
        /// Version of default repository
        /// </summary>
        public string version { get; set; }
        public string path { get; set; }
        public bool selected { get; set; }
        public string category { get; set; }
        public float size { get; set; }

        public StoredAsset()
        {
            Storage = _defaultStorage;
        }
    }

    enum Group { All, GroupByPublisher, SelectedOnly }
    
    [Flags]
    enum DrawFlag
    {
        publisher = 1,
        category = 2,
        name = 4,
        version = 8,
        size = 16
    }

    public static class CLI
    {
        /*
          AssetsManager -assets=.assets -action=Install -source=//127.0.0.1/shared_assets/
        */

        const string k_actionParam = "-action=";
        const string k_sourceParam = "-source=";
        const string k_assetsParam = "-assets=";

        static AssetManager assetManager = new AssetManager();

        public static void AssetsManager()
        {
            string action = GetParameterArgument(k_actionParam);

            switch (action)
            {
                case "install":

                    var storedAssets = assetManager.RefreshList();

                    assetManager.SelectAssets(
                        storedAssets,
                        assetManager.LoadAssets(Path.Combine(Environment.CurrentDirectory, ".assets")).Dependencies);

                    assetManager.InstallPackages(
                        storedAssets.Where(x => x.selected).ToList());

                    break;
                case "save": break;
                case "list": break;
            }
        }

        private static string GetParameterArgument(string parameterName)
        {
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg.ToLower().StartsWith(parameterName.ToLower()))
                {
                    return arg.Substring(parameterName.Length);
                }
            }
            return null;
        }
    }
}
