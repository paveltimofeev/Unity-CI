using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;

namespace Assets._CI.Editor
{
    class StoredAssetsView : EditorWindow
    {
        private static StoredAssetsView s_Instance;
        string assetsFilePath = string.Empty;

        private readonly GUIContent m_GUIRefreshList = new GUIContent("Refresh list", "Refresh list of downloaded assets");
        private readonly GUIContent m_GUIStoreAssets = new GUIContent("Save .assets", "Store selected assets to .assets list");
        private readonly GUIContent m_GUIImportAssets = new GUIContent("Import .assets", "Import assets from .assets list");
        private readonly GUIContent m_GUIInstallSelected = new GUIContent("Install selected", "Install selected packages");
        private readonly GUIContent m_GUIGroup = new GUIContent("View", "View mode");

        Hashtable publisher_table = new Hashtable();
        IList<StoredAsset> publishers = new List<StoredAsset>();
        List<StoredAsset> storedAssets = new List<StoredAsset>();
        Vector2 m_Scroll = Vector2.zero;
        Group m_Group = Group.GroupByPublisher;

        public void OnEnable()
        {
            titleContent = new GUIContent("Assets Manager");
            s_Instance = this;
            assetsFilePath = Path.Combine(Environment.CurrentDirectory, ".assets");

            RefreshList();
        }

        public void OnDestroy()
        {
            s_Instance = null;
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

            if (GUILayout.Button(m_GUIRefreshList, EditorStyles.toolbarButton))
            {
                Debug.Log(m_GUIRefreshList.text);
                RefreshList();
            }
            m_Group = (Group)EditorGUILayout.EnumPopup(m_Group, EditorStyles.toolbarDropDown);

            GUILayout.FlexibleSpace();

            EditorGUILayout.TextField("", EditorStyles.toolbarTextField);

            if (GUILayout.Button(m_GUIStoreAssets, EditorStyles.toolbarButton))
            {
                Debug.Log(m_GUIStoreAssets.text);
                var selected = storedAssets.Where(x => x.selected);
                SaveAssets( selected.ToList(), assetsFilePath );
            }

            if (GUILayout.Button(m_GUIImportAssets, EditorStyles.toolbarButton))
            {
                Debug.Log(m_GUIImportAssets.text);

                IList<StoredAsset> imported = LoadAssets(assetsFilePath);
                storedAssets.ForEach( a=> a.selected= false);
                foreach (var item in imported)
                {
                    storedAssets.Find(
                        x => 
                            x.version == item.version && 
                            x.publisher == item.publisher && 
                            x.name == item.name
                        
                        ).selected = true;   
                }
            }

            if (GUILayout.Button(m_GUIInstallSelected, EditorStyles.toolbarButton))
            {
                Debug.Log(m_GUIInstallSelected.text);
                var selected = storedAssets.Where(x => x.selected);
                InstallPackages(selected.ToList());
            }

            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAssetsList(IList<StoredAsset> assets)
        {
            EditorGUILayout.BeginVertical();
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(2000));

            if (assets != null)
            {
                foreach (StoredAsset publisher in publishers)
                {
                    if (publisher_table.ContainsKey(publisher.publisher))
                    {
                        //EditorGUILayout.Foldout((bool)publisher_table[publisher.publisher], publisher.publisher);

                        if (EditorGUILayout.Foldout((bool)publisher_table[publisher.publisher], publisher.publisher))
                        {
                            publisher_table[publisher.publisher] = true;
                            var publisherAssets = assets.Where(x => x.publisher == publisher.publisher).ToList();

                            foreach (StoredAsset asset in publisherAssets)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Space(10);
                                asset.selected = GUILayout.Toggle(asset.selected, string.Format("{0} [{1}]", asset.name, asset.version));
                                GUILayout.EndHorizontal();
                            }
                        }
                        else
                        {

                            publisher_table[publisher.publisher] = false;
                        }
                    }
                }

                /*
                foreach (StoredAsset asset in assets)
                {
                    asset.selected = GUILayout.Toggle(asset.selected, string.Format("[{1}] {2} / {0}", asset.name, asset.version, asset.publisher));
                }
                */
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }



        private IList<StoredAsset> CreatePublisherList(IList<StoredAsset> assets)
        {
            IList<StoredAsset> publishers = assets.GroupBy(x => x.publisher).Select(x => x.First()).ToList();
            publisher_table = new Hashtable();
            foreach (var item in publishers)
            {
                publisher_table[item.publisher] = false;
            }
            return publishers;
        }


        #region Toolbar Actions
        
        private void RefreshList()
        {
            // IF WIN
            storedAssets.Clear();

            string appdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity");
            string[] folders = Directory.GetDirectories(appdata, "Asset Store*");

            foreach (var folder in folders)
            {
                foreach (string package in Directory.GetFiles(folder, "*.unitypackage", SearchOption.AllDirectories))
                {
                    FileInfo packageInfo = new FileInfo(package);
                    storedAssets.Add(
                        new StoredAsset() {
                            path = package,
                            name = Path.GetFileNameWithoutExtension(package),
                            publisher = packageInfo.Directory.Parent.Name,
                            category = packageInfo.Directory.Name,
                            version = Path.GetFileNameWithoutExtension(folder)
                        });
                }
            }

            publishers = CreatePublisherList(storedAssets);
        }

        public void SaveAssets(IList<StoredAsset> assets, string filePath)
        {
            StringBuilder content = new StringBuilder();
            foreach (StoredAsset asset in assets)
            {
                content.AppendFormat("{0};{1};{2}\r\n", asset.version, asset.publisher, asset.name);
            }

            Debug.Log("Save content to " + filePath);

            File.WriteAllText(assetsFilePath, content.ToString());
        }

        public IList<StoredAsset> LoadAssets(string filePath)
        {
            Debug.Log("Load assets from " + filePath);

            string[] lines = File.ReadAllLines(assetsFilePath);
            IList<StoredAsset> result = new List<StoredAsset>();
            foreach (string data in lines)
            {
                result.Add(new StoredAsset() { 
                    version = data.Split(';')[0], 
                    publisher = data.Split(';')[1], 
                    name = data.Split(';')[2] });
            }

            return result;
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
        
        #endregion


        [MenuItem("Window/Assets Manager", false, 116)]
        static void LoadThirdPartyAssets()
        {
            Debug.Log("'CI -> Load stored assets");

            GetWindow(typeof(StoredAssetsView)).Show();

        }
    }

    class StoredAsset
    {
        public string name;
        public string publisher;
        public string version;
        public string path;
        public bool selected;
        public string category;
    }

    enum Group { GroupByPublisher, SelectedOnly, Filtered, All }
}
