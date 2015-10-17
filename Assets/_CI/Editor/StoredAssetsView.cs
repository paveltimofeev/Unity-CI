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
    // Views 

    class BaseView : EditorWindow
    {
        public virtual void OnEnable() { }
        public virtual void OnDestroy() { }
        public virtual void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawToolbar();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical();
            DrawContent();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        protected virtual void DrawToolbar() { }
        protected virtual void DrawContent() { }

        protected virtual void DrawToolbarButton(GUIContent content, Action action)
        {
            if (GUILayout.Button(content, EditorStyles.toolbarButton))
                action();
        }
    }

    class StoredAssetsView : BaseView
    {
        AssetManager assetManager = new AssetManager();
        AseetsConfig config = null;
        IList<StoredAsset> publishers = new List<StoredAsset>();
        List<StoredAsset> storedAssets = new List<StoredAsset>();
        
        #region GUI fields
        private readonly GUIContent m_GUIRefreshList = new GUIContent("Refresh list", "Refresh list of downloaded assets");
        private readonly GUIContent m_GUIStoreAssets = new GUIContent("Save .assets", "Store selected assets to .assets list");
        private readonly GUIContent m_GUIImportAssets = new GUIContent("Import .assets", "Import assets from .assets list");
        private readonly GUIContent m_GUIInstallSelected = new GUIContent("Install selected", "Install selected packages");
        private Hashtable publisher_foldout_table = new Hashtable();
        private Vector2 m_Scroll = Vector2.zero;
        private Group m_Group = Group.GroupByPublisher;
        #endregion

        private string assetsFilePath = string.Empty;
        private string m_searchFilter = "";


        public override void OnEnable()
        {
            titleContent = new GUIContent("Assets Manager");
            assetsFilePath = Path.Combine(Environment.CurrentDirectory, ".assets");

            config = assetManager.LoadConfig(assetsFilePath);
            storedAssets = assetManager.GetAssetsList(config);
        }

        protected override void DrawToolbar()
        {
            DrawToolbarButton(m_GUIRefreshList, () =>
            {
                config = assetManager.LoadConfig(assetsFilePath);
                storedAssets = assetManager.GetAssetsList(config);
                publishers = assetManager.GetPublisherList(storedAssets);

                l.og(m_GUIRefreshList.text + " -> Dependencies {0}", config.Dependencies.Count);
                l.og(m_GUIRefreshList.text + " -> Default Source {0}", config.DefaultSource.Location);
                l.og(m_GUIRefreshList.text + " -> Total Sources {0}", config.Sources.Count);
                l.og(m_GUIRefreshList.text + " -> Total Assets {0}", storedAssets.Count);
                l.og(m_GUIRefreshList.text + " -> Total Publishers {0}", publishers.Count);

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
                var selected = GetSelectedItems();
                l.og(m_GUIStoreAssets.text + " -> Selected {0}", selected.Count);
                
                assetManager.SaveConfig(selected, assetsFilePath);
            });

            DrawToolbarButton(m_GUIImportAssets, () =>
            {
                IList<StoredAsset> imported = assetManager.LoadConfig(assetsFilePath).Dependencies;
                l.og(m_GUIImportAssets.text + " -> Imported {0}", imported.Count);

                assetManager.MarkSelected(storedAssets, imported);
            });

            DrawToolbarButton(m_GUIInstallSelected, () =>
            {
                var selected = GetSelectedItems();
                l.og(m_GUIImportAssets.text + " -> Installing {0}", selected.Count);

                assetManager.InstallPackages(selected);
            });
        }

        protected override void DrawContent()
        {
            IList<StoredAsset> assets = storedAssets;

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

        private IList<StoredAsset> GetSelectedItems()
        {
            return storedAssets.Where(x => x.selected).ToList();
        }

        [MenuItem("Window/Assets Manager", false, 116)]
        private static void LoadThirdPartyAssets()
        {
            GetWindow(typeof(StoredAssetsView)).Show();
        }
    }

    class ManageSourcesView : BaseView
    {
        AssetManager assetManager = new AssetManager();
        AseetsConfig config = null;
        private string assetsFilePath = string.Empty;
        private string newAssetSource = "";
        private string[] sources = new string[0];

        private GUIContent m_GUISave = new GUIContent("Save", "");

        public override void OnEnable()
        {
            titleContent = new GUIContent("Sources");
            assetsFilePath = Path.Combine(Environment.CurrentDirectory, ".assets");
            config = assetManager.LoadConfig(assetsFilePath);
            sources = new string[config.Sources.Count];
            
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = config.Sources[i].Location;
            }
        }

        protected override void DrawToolbar()
        {
            DrawToolbarButton(m_GUISave, () =>
            {
                config.Sources = new List<AssetSource>();
                for (int i = 0; i < sources.Length; i++)
                {
                    config.Sources.Add(new AssetSource() {
                        Name = i.ToString(),
                        Location = sources[i]
                    });    
                }

                assetManager.SaveConfig(config, assetsFilePath);
            });
        }
        
        protected override void DrawContent()
        {
            for (int i = 0; i < sources.Length; i++)
            {
                DrawItem(sources[i], "x",
                        (string val) => // change
                        {
                            sources[i] = val; 
                        },
                        (string val) => // click on 'x'
                        { 
                            sources[i] = "~"; // removing
                        });
            }

            DrawItem(newAssetSource, "+", 
                    (string val) => // change
                    {
                        newAssetSource = val; 
                    },
                    (string val) => // click on '+'
                    {
                        Array.Resize(ref sources, sources.Length + 1);
                        sources[sources.Length-1] = val;
                        newAssetSource = "";
                    });

            sources = sources.Where( x => x != "~").ToArray();
        }

        private void DrawItem(string value, string button,  Action<string> onChange, Action<string> onClick)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Assets Location: ", GUILayout.Width(120));
            value = GUILayout.TextField(value);
            if (button != null && GUILayout.Button(button, EditorStyles.miniButton, GUILayout.Width(20)))
                onClick(value);
            else
                onChange(value);
            GUILayout.EndHorizontal();
        }

        [MenuItem("Window/Manage Sources", false, 117)]
        private static void LoadThirdPartyAssets()
        {
            GetWindow(typeof(ManageSourcesView)).Show();
        }
    }

    // Logic

    class AssetManager
    {
        private const string k_appUnityFolder = "Unity";
        private const string k_assetsFolderPattern = "Asset Store*";
        private const string k_assetsFilePattern = "*.unitypackage";

        public List<StoredAsset> GetAssetsList(AseetsConfig config)
        {
            // IF WIN
            //string appdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), k_appUnityFolder);

            return GetAssetsList(config.DefaultSource);
        }
        public List<StoredAsset> GetAssetsList(AssetSource source)
        {
            List<StoredAsset> storedAssets = new List<StoredAsset>();

            string[] folders = Directory.GetDirectories(source.Location, k_assetsFolderPattern);

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

            l.og("Got Assets List with {0} items", storedAssets.Count);

            return storedAssets;
        }

        public IList<StoredAsset> GetPublisherList(IList<StoredAsset> assets)
        {
            var result = assets.GroupBy(x => x.publisher).Select(x => x.First()).ToList();
            l.og("Got Publisher List with {0} items (sorted across {1} assets)", result.Count, assets.Count);
            return result;
        }

        public AseetsConfig LoadConfig(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            AseetsConfig config = new AseetsConfig();
            config.DefaultSource = new AssetSource() { Name = "Default", Location = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), k_appUnityFolder) };

            foreach (string data in lines)
            {
                char first = data.Length > 0 ? data[0] : '#';
                switch (first)
                {
                    case '#': /// comment line
                        break;
                    case '~': /// Assets source declaration
                        IParser parser = new AssetSourceParser();
                        parser.Parse(data);
                        config.Sources.Add(new AssetSource() { Name = parser.Parsed[1], Location = parser.Parsed[2] });
                        break;

                    default: /// Asset declaration
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
        public void SaveConfig(IList<StoredAsset> assets, string filePath)
        {
            SaveConfig(new AseetsConfig(){ Dependencies = assets }, filePath);
        }
        public void SaveConfig(AseetsConfig config, string filePath)
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

        public void InstallPackages(IList<StoredAsset> assets)
        {
            foreach (var asset in assets)
            {
                l.og("Install '{0}'", asset.path);
                AssetDatabase.ImportPackage(asset.path, false);
            }
        }
        public void MarkSelected(List<StoredAsset> storedAssets, IList<StoredAsset> imported)
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
    }

    #region Parsers
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
    #endregion

    // Models

    #region POCOs
    class AseetsConfig
    {
        public IList<AssetSource> Sources { get; set; }
        public IList<StoredAsset> Dependencies { get; set; }

        public AseetsConfig()
        {
            Sources = new List<AssetSource>();
            Dependencies = new List<StoredAsset>();
        }

        public AssetSource DefaultSource { get; set; }
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

    enum Group { All, GroupByPublisher, GroupBySource, SelectedOnly }
    
    [Flags]
    enum DrawFlag
    {
        publisher = 1,
        category = 2,
        name = 4,
        version = 8,
        size = 16
    }
    #endregion

    // Utils

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

                    var config = assetManager.LoadConfig(Path.Combine(Environment.CurrentDirectory, ".assets"));
                    var storedAssets = assetManager.GetAssetsList(config);

                    assetManager.MarkSelected(storedAssets, config.Dependencies);

                    assetManager.InstallPackages(storedAssets.Where(x => x.selected).ToList());

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

    public class l
    {
        public static void og(string format, params object[] args)
        {
            Debug.Log(string.Format(format, args));
        }
    }
}
