#region Author
/************************************************************************************************************
Author: Nidre (Erdin Kacan)
Website: http://erdinkacan.tumblr.com/
GitHub: https://github.com/Nidre
Behance : https://www.behance.net/erdinkacan
************************************************************************************************************/
#endregion
#region Copyright
/************************************************************************************************************
The MIT License (MIT)
Copyright (c) 2015 Erdin
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
************************************************************************************************************/

#endregion
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
[Serializable]
public class Builder : EditorWindow
{
    // How to save the configuration file.
    enum SaveMode
    {
        AsDefault,
        Overwrite,
        SaveAs
    }

    // How to load the configuration file.
    enum LoadMode
    {
        Default,
        AskForPath,
        Args
    }

    // Sort mode for the build list.
    enum SortMode
    {
        None,
        ByPlatform,
        ByName,
        ById,
    }

    // Sort direction of for the sort mode.
    enum SortDirection
    {
        Ascending,
        Descending
    }

    // Holds a single build configuration.
    [Serializable]
    class BuildConfiguration
    {
        public BuildConfiguration()
        {
            this.options = _defaultBuildOptions;
            this.uniqueId = UnityEngine.Random.Range(0, 1000000).ToString("000000");
        }

        public BuildConfiguration(BuildTarget target, BuildOptions options)
        {
            this.target = target;
            this.options = options;
            this.uniqueId = UnityEngine.Random.Range(0, 1000000).ToString("000000");
        }

        public BuildConfiguration Copy()
        {
            BuildConfiguration bc = new BuildConfiguration();
            bc.options = options;
            bc.target = target;
            bc.scenes = scenes;
            bc.scenesToggle = scenesToggle;
            bc.enabled = enabled;
            bc.name = name;
            return bc;
        }

        public BuildOptions options = BuildOptions.None;
        public BuildTarget target = BuildTarget.Android;
        public List<Scene> scenes = new List<Scene>();
        public bool scenesToggle = true;
        public bool toggle = true;
        public bool enabled = true;
        public string name = "No Name";
        public string uniqueId;
    }

    // Folder to output our builds.
    static string _mainBuildPath;

    // Holds scene information
    [Serializable]
    struct Scene
    {
        public string path;
        public string name;
    }

    // List of scenes
    static Scene[] _sceneList;
    // List of builds
    static List<BuildConfiguration> _builds;
    // Scrollbar position
    static Vector2 _scrollPosition;
    // Most recent configuration.
    static string mostRecentConfiguration;
    // Current sort mode.
    static SortMode _sortMode;
    // Current sort direction.
    static SortDirection _sortDirection;
    static EditorWindow window;
    // Default builds options to use when creating a new build configuration.
    static BuildOptions _defaultBuildOptions;
    // Should we return back to initial configuration after builds completed.
    static bool _resetTarget;
    // Pretty clear?
    static bool isBuilding;
    // Small delay after the build. Probably not necessary.
    static float delayEnd;
    // Which configration we are currently iterating.
    static int buildIndex;
    // Well. Timetamp of build.
    static string _timeStamp;
    // Timestamp format.
    static string _dateTimeFormat = "yy.MM.dd";
    // Toggle variable for Foldout UI.
    static bool _sortUtilsToggle = true;
    static bool _toolOptionsToggle = true;
    static bool _listUtilsToggle = true;

    //Nasty bug thingy
    static bool isScrollAlive;

    [MenuItem("Window/Builder")]
    static void ShowWindow()
    {
        if (_builds != null) _builds.Clear();
        //Load scenes
        RefreshSceneList();

        //Load settings and default configuration
        LoadToolSettings();
        LoadSettings(LoadMode.Default);

        //Load Failed
        if (_builds == null)
        {
            _builds = new List<BuildConfiguration>();
            _builds.Add(new BuildConfiguration());
            SaveSettings(SaveMode.AsDefault);
        }
        window = (Builder)EditorWindow.GetWindow(typeof(Builder));
    }

    static void CommandLineBuild()
    {
        RefreshSceneList();
        LoadToolSettings();
        LoadSettings(LoadMode.Args);
        Build();
    }

    /// <summary>
    /// Load scenes included in build options.
    /// This method might cause loss of data if scenes are renamed or reordered.
    /// </summary>
    static void RefreshSceneList()
    {
        List<Scene> temp = new List<Scene>();
        foreach (UnityEditor.EditorBuildSettingsScene scene in UnityEditor.EditorBuildSettings.scenes)
        {
            string sceneName = scene.path.Substring(scene.path.LastIndexOf('/') + 1);
            sceneName = sceneName.Substring(0, sceneName.Length - 6);
            Scene sceneObj = new Scene()
            {
                path = scene.path,
                name = sceneName
            };
            temp.Add(sceneObj);
        }
        _sceneList = temp.OrderBy(scene => scene.name).ToArray();
    }

    /// <summary>
    /// Saves the configurations.
    /// </summary>
    /// <param name="mode">Save Mode.</param>
    static void SaveSettings(SaveMode mode)
    {
        Dictionary<string, object> settings = new Dictionary<string, object>();
        settings.Add("mainBuildPath", _mainBuildPath);
        settings.Add("buildCount", _builds.Count);
        settings.Add("sortBy", _sortMode);
        settings.Add("sortDirection", _sortDirection);
        settings.Add("resetTarget", _resetTarget);

        Dictionary<string, object> build;
        for (int i = 0; i < _builds.Count; i++)
        {
            BuildConfiguration bc = _builds[i];
            build = new Dictionary<string, object>();
            build.Add("name", bc.name);

            string[] scenePaths = new string[bc.scenes.Count];
            for (int p = 0; p < bc.scenes.Count; p++)
            {
                scenePaths[p] = bc.scenes[p].path;
            }

            build.Add("scenes", scenePaths);
            build.Add("target", bc.target);
            build.Add("options", bc.options);
            build.Add("scenesToggle", bc.scenesToggle);
            build.Add("toggle", bc.toggle);
            build.Add("enabled", bc.enabled);
            build.Add("uniqueId", bc.uniqueId);
            settings.Add("build" + i, build);
        }
        switch (mode)
        {
            case SaveMode.AsDefault:
                mostRecentConfiguration = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity/Nidre/Builder/Default.ini");
                break;
            case SaveMode.Overwrite:
                break;
            case SaveMode.SaveAs:
                string temp = EditorUtility.SaveFilePanel("Save Configuration", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity/Nidre/Builder").ToString(), "", "ini");
                if (!string.IsNullOrEmpty(mostRecentConfiguration))
                {
                    mostRecentConfiguration = temp;
                }
                break;
        }

        if (!string.IsNullOrEmpty(mostRecentConfiguration))
        {
            using (StreamWriter writer = new StreamWriter(mostRecentConfiguration))
            {
                writer.WriteLine(MiniJSON.Json.Serialize(settings));
            }
        }
        else
        {
            if (EditorUtility.DisplayDialog("Can't save configuration!", "Invalid file path", "Cancel", "Retry"))
            {

            }
            else
            {
                SaveSettings(mode);
            }
        }

    }

    /// <summary>
    /// Loads the configurations.
    /// </summary>
    static void LoadSettings(LoadMode loadMode)
    {
        string dirPath;
        string filePath = null;
        switch (loadMode)
        {
            case LoadMode.Default:
                dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity/Nidre/Builder");
                filePath = Path.Combine(dirPath, "Default.ini");
                // Create initial golder structure
                if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
                // Save default settings
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Close();
                    _mainBuildPath = null;
                    _builds = new List<BuildConfiguration>();
                    _builds.Add(new BuildConfiguration());
                    SaveSettings(SaveMode.AsDefault);
                }
                break;
            case LoadMode.AskForPath:
                dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity/Nidre/Builder");
                filePath = EditorUtility.OpenFilePanel("Load Configuration", dirPath, "ini");
                break;
            case LoadMode.Args:
                string[] args = Environment.GetCommandLineArgs();
                if (args != null && args.Length > 1)
                {
                    filePath = CommandLineReader.GetCustomArgument("confPath");
                    if (!File.Exists(filePath))
                    {
                        throw new FileNotFoundException("Configuration file not found" + filePath, filePath);
                    }
                }
                else
                {
                    throw new ArgumentException("Arguments missing.", "filePath");
                }
                break;
        }
        string serialized = System.IO.File.ReadAllText(filePath);

        if (serialized != null)
        {
            Dictionary<string, object> deserialized = MiniJSON.Json.Deserialize(serialized) as Dictionary<string, object>;
            try
            {
                _mainBuildPath = deserialized["mainBuildPath"] as string;
                _builds = new List<BuildConfiguration>(int.Parse(deserialized["buildCount"].ToString()));
                _sortMode = (SortMode)Enum.Parse(typeof(SortMode), deserialized["sortBy"] as string);
                _sortDirection = (SortDirection)Enum.Parse(typeof(SortDirection), deserialized["sortDirection"] as string);
                _resetTarget = (bool)deserialized["resetTarget"];

                for (int i = 0; i < _builds.Capacity; i++)
                {
                    BuildConfiguration bc = new BuildConfiguration();
                    Dictionary<string, object> buildConf = deserialized["build" + i] as Dictionary<string, object>;
                    List<object> temp = buildConf["scenes"] as List<object>;
                    bc.scenes = new List<Scene>();
                    foreach (var item in temp)
                    {
                        string scenePath = item as string;
                        string sceneName = scenePath.Substring(scenePath.LastIndexOf('/') + 1);
                        sceneName = sceneName.Substring(0, sceneName.Length - 6);
                        Scene newScene = new Scene()
                        {
                            path = scenePath,
                            name = sceneName

                        };
                        bc.scenes.Add(newScene);
                    }
                    bc.name = buildConf["name"] as string;
                    bc.options = (BuildOptions)Enum.Parse(typeof(BuildOptions), buildConf["options"] as string);
                    bc.target = (BuildTarget)Enum.Parse(typeof(BuildTarget), buildConf["target"] as string);
                    bc.scenesToggle = (bool)buildConf["scenesToggle"];
                    bc.toggle = (bool)buildConf["toggle"];
                    bc.enabled = (bool)buildConf["enabled"];
                    bc.uniqueId = int.Parse(buildConf["uniqueId"] as string).ToString("000000");
                    _builds.Add(bc);
                }
                ApplySort();
                serialized = null;
                mostRecentConfiguration = filePath;
            }
            catch (Exception e)
            {
                serialized = null;
                if (e.GetType() == typeof(ArgumentException) ||
                    e.GetType() == typeof(ArgumentNullException) ||
                    e.GetType() == typeof(ArgumentOutOfRangeException) ||
                    e.GetType() == typeof(NullReferenceException) ||
                    e.GetType() == typeof(KeyNotFoundException))
                {
                    switch (loadMode)
                    {
                        case LoadMode.Default:
                            int dialogResult = EditorUtility.DisplayDialogComplex("Can't read configuration!", "File seems to be corrupt", "Cancel", "Reset", "Retry");
                            if (dialogResult == 1)
                            {
                                _builds = new List<BuildConfiguration>();
                                _builds.Add(new BuildConfiguration());
                                SaveSettings(SaveMode.AsDefault);
                            }
                            else if (dialogResult == 2)
                            {
                                LoadSettings(loadMode);
                            }
                            break;
                        case LoadMode.AskForPath:
                            if (!EditorUtility.DisplayDialog("Can't read configuration!", "File seems to be corrupt", "Cancel", "Retry"))
                            {
                                LoadSettings(loadMode);
                            }
                            break;
                        case LoadMode.Args:
                            throw new FileLoadException("Can't read configuration!");
                    }
                }
                else Debug.LogException(e);
            }
        }
        else
        {
            Debug.LogError("Serilized text is Null");
        }
    }

    /// <summary>
    /// Saves the tool settings.
    /// </summary>
    static void SaveToolSettings()
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity/Nidre/Builder/Settings.ini");

        Dictionary<string, object> settings = new Dictionary<string, object>();

        settings.Add("defaultBuildOptions", _defaultBuildOptions);
        settings.Add("sortUtilsToggle", _sortUtilsToggle);
        settings.Add("toolOptionsToggle", _toolOptionsToggle);
        settings.Add("listUtilsToggle", _listUtilsToggle);

        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine(MiniJSON.Json.Serialize(settings));
        }
    }

    /// <summary>
    /// Loads the tool settings.
    /// </summary>
    static void LoadToolSettings()
    {
        string dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity/Nidre/Builder");
        string filePath = Path.Combine(dirPath, "Settings.ini");

        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close();
            SaveToolSettings();
        }
        string serialized;
        using (StreamReader reader = new StreamReader(filePath))
        {
            serialized = reader.ReadToEnd();
        }

        if (serialized != null)
        {
            Dictionary<string, object> deserialized = MiniJSON.Json.Deserialize(serialized) as Dictionary<string, object>;
            try
            {
                _defaultBuildOptions = (BuildOptions)Enum.Parse(typeof(BuildOptions), deserialized["defaultBuildOptions"] as string);
                _sortUtilsToggle = (bool)deserialized["sortUtilsToggle"];
                _toolOptionsToggle = (bool)deserialized["toolOptionsToggle"];
                _listUtilsToggle = (bool)deserialized["listUtilsToggle"];
            }
            catch (Exception e)
            {
                serialized = null;
                if (e.GetType() == typeof(ArgumentException) ||
                    e.GetType() == typeof(ArgumentNullException) ||
                    e.GetType() == typeof(ArgumentOutOfRangeException) ||
                    e.GetType() == typeof(NullReferenceException) ||
                    e.GetType() == typeof(KeyNotFoundException))
                {
                    _defaultBuildOptions = BuildOptions.None;
                    SaveToolSettings();
                }
                else Debug.LogException(e);
            }
        }
    }

    /// <summary>
    /// Resets the settings.
    /// </summary>
    static void ResetSettings()
    {
        _builds = null;
        _mainBuildPath = null;
        _builds = new List<BuildConfiguration>();
        _builds.Add(new BuildConfiguration());
        SaveSettings(SaveMode.AsDefault);
        _defaultBuildOptions = BuildOptions.None;
        SaveToolSettings();
    }

    /// <summary>
    /// Selects the build path.
    /// </summary>
    static void SelectBuildPath()
    {
        if (!string.IsNullOrEmpty(_mainBuildPath))
        {
            try
            {
                Path.GetDirectoryName(_mainBuildPath);
            }
            catch
            {
                if (EditorUtility.DisplayDialog("Folder path ureachable", "Please enter a valid path for build.", "Select Folder", "Cancel"))
                {
                    _mainBuildPath = EditorUtility.OpenFolderPanel("Select folder", EditorApplication.applicationPath, "");
                    SelectBuildPath();
                }
            }
        }
    }

    void OnGUI()
    {
        if (!isBuilding && (Time.realtimeSinceStartup > delayEnd) && !UnityEditor.BuildPipeline.isBuildingPlayer)
        {
            if (_builds != null)
            {
                #region BuilderUtils
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save", EditorStyles.miniButtonLeft))
                {
                    SaveSettings(SaveMode.Overwrite);
                }
                if (GUILayout.Button("Load", EditorStyles.miniButtonRight))
                {
                    LoadSettings(LoadMode.AskForPath);
                }
                EditorGUILayout.Space();
                if (GUILayout.Button("Save as Default", EditorStyles.miniButtonLeft))
                {
                    SaveSettings(SaveMode.AsDefault);
                }
                if (GUILayout.Button("Save As..", EditorStyles.miniButtonMid))
                {
                    SaveSettings(SaveMode.SaveAs);
                }
                if (GUILayout.Button("Load Default", EditorStyles.miniButtonRight))
                {
                    LoadSettings(LoadMode.Default);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh Scenes", EditorStyles.miniButton))
                {
                    int dialogResult = EditorUtility.DisplayDialogComplex("Refrest Scenes", "You may lose data if scenes names are modified. It is highly recommended to save configuration before refresh.", "Save and Refresh", " Just Refresh", "Cancel");

                    switch (dialogResult)
                    {
                        case 0:
                            SaveSettings(SaveMode.Overwrite);
                            RefreshSceneList();
                            break;
                        case 1:
                            RefreshSceneList();
                            break;
                    }
                }
                if (GUILayout.Button("Reset Settings", EditorStyles.miniButton))
                {
                    if (EditorUtility.DisplayDialog("Delete All Settings", "This will delete all settings and reset the tool to it's default settings as it was on first use", "Ok", "Cancel"))
                    {
                        ResetSettings();
                    }
                }
                EditorGUILayout.EndHorizontal();
                #endregion

                EditorGUILayout.Space();

                bool prevToggle = _toolOptionsToggle;
                _toolOptionsToggle = EditorGUILayout.Foldout(_toolOptionsToggle, "Tool Options");
                if (!prevToggle.Equals(_toolOptionsToggle)) SaveToolSettings();
                if (_toolOptionsToggle)
                {
                    _resetTarget = EditorGUILayout.ToggleLeft("Switch to " + EditorUserBuildSettings.activeBuildTarget + "(Current) when completed.", _resetTarget);
                }

                EditorGUILayout.Space();

                #region SortUtils

                prevToggle = _sortUtilsToggle;
                _sortUtilsToggle = EditorGUILayout.Foldout(_sortUtilsToggle, "Sort Options");
                if (!prevToggle.Equals(_sortUtilsToggle)) SaveToolSettings();
                if (_sortUtilsToggle)
                {
                    EditorGUILayout.LabelField("Sort", EditorStyles.boldLabel, null);

                    EditorGUILayout.BeginHorizontal();
                    _sortMode = (SortMode)EditorGUILayout.EnumPopup("", _sortMode, GUILayout.Width(100));
                    _sortDirection = (SortDirection)EditorGUILayout.EnumPopup("", _sortDirection, GUILayout.Width(100));
                    GUI.enabled = _sortMode != SortMode.None;
                    if (GUILayout.Button("Sort", EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        ApplySort();
                    }
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                }
                #endregion

                EditorGUILayout.Space();

                #region ListUtils

                prevToggle = _listUtilsToggle;
                _listUtilsToggle = EditorGUILayout.Foldout(_listUtilsToggle, "List Options");
                if (!prevToggle.Equals(_listUtilsToggle)) SaveToolSettings();
                if (_listUtilsToggle)
                {
                    EditorGUILayout.LabelField("List Controls", EditorStyles.boldLabel, null);

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Colapse All", EditorStyles.miniButtonLeft))
                    {
                        for (int i = 0; i < _builds.Count; i++)
                        {
                            _builds[i].toggle = false;
                        }
                    }
                    if (GUILayout.Button("Expand All", EditorStyles.miniButtonRight))
                    {
                        for (int i = 0; i < _builds.Count; i++)
                        {
                            _builds[i].toggle = true;
                        }
                    }
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Enable All", EditorStyles.miniButtonLeft))
                    {
                        for (int i = 0; i < _builds.Count; i++)
                        {
                            _builds[i].enabled = true;
                        }
                    }
                    if (GUILayout.Button("Disable All", EditorStyles.miniButtonRight))
                    {
                        for (int i = 0; i < _builds.Count; i++)
                        {
                            _builds[i].enabled = false;
                        }
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Clear", EditorStyles.miniButton))
                    {
                        _builds.Clear();
                        _builds.Add(new BuildConfiguration());
                    }
                    EditorGUILayout.EndHorizontal();
                }
                #endregion

                EditorGUILayout.Space();


                EditorGUILayout.LabelField("Build Queue", EditorStyles.boldLabel, null);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                isScrollAlive = true;
                for (int i = 0; i < _builds.Count; i++)
                {
                    EditorGUI.indentLevel = 0;
                    #region Build
                    BuildConfiguration bc = _builds[i];
                    string selectedScenes = string.Empty;
                    if (bc.scenes.Count > 0)
                    {
                        selectedScenes = "(";
                        bc.scenes = bc.scenes.OrderBy(scene => scene.name).ToList();
                        for (int s = 0; s < bc.scenes.Count; s++)
                        {
                            selectedScenes += bc.scenes[s].name;
                            if (s < bc.scenes.Count - 1)
                            {
                                selectedScenes += ",";
                            }
                        }
                        selectedScenes += ")";
                    }

                    #region Title
                    EditorGUILayout.BeginHorizontal();
                    bc.enabled = EditorGUILayout.ToggleLeft("", bc.enabled, GUILayout.Width(10));
                    Rect r = GUILayoutUtility.GetLastRect();
                    r.width = 65;
                    r.x += 15;
                    bc.toggle = EditorGUI.Foldout(r, bc.toggle, bc.uniqueId, true);

                    GUILayout.Space(65);
                    bc.name = EditorGUILayout.TextField(bc.name);
                    bc.target = (BuildTarget)EditorGUILayout.EnumPopup("", bc.target, GUILayout.MaxWidth(150));
                    EditorGUILayout.SelectableLabel(selectedScenes);
                    GUILayout.FlexibleSpace();

                    BuildConfListUtils(i);

                    EditorGUILayout.EndHorizontal();
                    #endregion

                    if (bc.toggle)
                    {
                        EditorGUILayout.BeginVertical();
                        GUI.enabled = bc.enabled;

                        EditorGUI.indentLevel++;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Build Options", GUILayout.Width(90));
                        bc.options = (BuildOptions)EditorGUILayout.EnumMaskField("", bc.options, GUILayout.Width(150));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Make default"))
                        {
                            _defaultBuildOptions = bc.options;
                            SaveToolSettings();
                        }
                        EditorGUILayout.EndHorizontal();

                        #region SceneList
                        EditorGUILayout.BeginVertical();
                        bc.scenesToggle = EditorGUILayout.Foldout(bc.scenesToggle, "Scenes " + selectedScenes);
                        EditorGUI.indentLevel++;
                        if (bc.scenesToggle)
                        {
                            for (int k = 0; k < _sceneList.Length; k++)
                            {
                                if (EditorGUILayout.ToggleLeft(_sceneList[k].name, bc.scenes.Contains(_sceneList[k])))
                                {
                                    if (!bc.scenes.Contains(_sceneList[k]))
                                    {
                                        bc.scenes.Add(_sceneList[k]);
                                    }
                                }
                                else
                                {
                                    bc.scenes.Remove(_sceneList[k]);
                                }
                            }
                        }
                        EditorGUILayout.EndVertical();
                        #endregion
                        EditorGUILayout.EndVertical();
                    }
                    #endregion
                }

                #region Build
                GUI.enabled = _builds.Count(build => build.enabled == true) > 0;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Build Folder : ", GUILayout.Width(95));
                if (string.IsNullOrEmpty(_mainBuildPath))
                {
                    GUILayout.Label("Choose Folder", GUILayout.MaxWidth(window.position.width - 250));
                    if (GUILayout.Button("...", EditorStyles.miniButtonLeft, GUILayout.Width(25))) { _mainBuildPath = EditorUtility.OpenFolderPanel("Select folder", _mainBuildPath, _mainBuildPath) + "/"; }
                    GUI.enabled = false;
                }
                else
                {
                    GUILayout.Label(_mainBuildPath, GUILayout.MaxWidth(window.position.width - 250));
                    if (GUILayout.Button("...", EditorStyles.miniButtonLeft, GUILayout.Width(25))) { EditorUtility.OpenFolderPanel("Select folder", EditorApplication.applicationPath, ""); }
                }

                if (GUILayout.Button("Browse", EditorStyles.miniButtonRight, GUILayout.Width(100))) { EditorUtility.RevealInFinder(_mainBuildPath); }
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("Start Build", GUILayout.Width(100)))
                {
                    isBuilding = true;
                    SelectBuildPath();
                    EditorGUILayout.EndScrollView();
                    Build();
                    isScrollAlive = false;
                    return;
                }
                GUILayout.FlexibleSpace();
                GUI.enabled = true;
                #endregion

            }
            if (isScrollAlive) EditorGUILayout.EndScrollView();
        }

        if (window) EditorUtility.SetDirty(window);
    }

    /// <summary>
    /// Applies the sort.
    /// </summary>
    private static void ApplySort()
    {
        switch (_sortMode)
        {
            case SortMode.ByPlatform:
                if (_sortDirection == SortDirection.Ascending) _builds = _builds.OrderBy(build => build.target).ToList();
                else _builds = _builds.OrderByDescending(build => build.target).ToList();
                break;
            case SortMode.ByName:
                if (_sortDirection == SortDirection.Ascending) _builds = _builds.OrderBy(build => build.name).ToList();
                else _builds = _builds.OrderByDescending(build => build.name).ToList();
                break;
            case SortMode.ById:
                if (_sortDirection == SortDirection.Ascending) _builds = _builds.OrderBy(build => build.uniqueId).ToList();
                else _builds = _builds.OrderByDescending(build => build.uniqueId).ToList();
                break;
        }
    }

    /// <summary>
    /// Displays the build configuration list utilities.
    /// </summary>
    /// <param name="i">The build index.</param>
    static void BuildConfListUtils(int i)
    {
        GUI.enabled = i > 0;
        if (GUILayout.Button("Up", EditorStyles.miniButtonLeft, GUILayout.Width(40)))
        {
            BuildConfiguration bc = _builds[i];
            _builds.RemoveAt(i);
            _builds.Insert(i - 1, bc);
        }
        GUI.enabled = true;
        if (GUILayout.Button("Copy", EditorStyles.miniButtonMid, GUILayout.Width(40)))
        {
            BuildConfiguration bc = _builds[i].Copy();
            string initialID = bc.uniqueId;
            int index = 0;
            while (index < _builds.Count - 1)
            {
                if (_builds[index].uniqueId.Equals(bc.uniqueId))
                {
                    bc.uniqueId = UnityEngine.Random.Range(0, 1000000).ToString("000000");
                    index = 0;
                }
                else index++;
            }
            _builds.Insert(i + 1, bc);
        }
        GUI.enabled = i < _builds.Count - 1;
        if (GUILayout.Button("Down", EditorStyles.miniButtonRight, GUILayout.Width(40)))
        {
            BuildConfiguration bc = _builds[i];
            _builds.RemoveAt(i);
            _builds.Insert(i + 1, bc);
        }
        GUI.enabled = true;
        if (GUILayout.Button("+", EditorStyles.miniButtonLeft, GUILayout.Width(25)))
        {
            BuildConfiguration bc = new BuildConfiguration();
            string initialID = bc.uniqueId;
            int index = 0;
            while (index < _builds.Count - 1)
            {
                if (_builds[index].uniqueId.Equals(bc.uniqueId))
                {
                    bc.uniqueId = UnityEngine.Random.Range(0, 1000000).ToString("000000");
                    index = 0;
                }
                else index++;
            }
            _builds.Insert(i + 1, bc);
        }
        GUI.enabled = _builds.Count > 1;
        if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(25)))
        {
            _builds.RemoveAt(i);
        }
        GUI.enabled = true;
    }

    /// <summary>
    /// Iterates and build each item in build list.
    /// May reorder list if _resetTarget is true to make sure we return back to initial configration in most optimal way.
    /// </summary>
    static void Build()
    {
        BuildTarget _initBuildTarget = EditorUserBuildSettings.activeBuildTarget;
        List<BuildConfiguration> _toBeBuild = new List<BuildConfiguration>();
        _toBeBuild.AddRange(_builds.ToArray());

        _timeStamp = DateTime.Now.ToString(_dateTimeFormat);
        while (_toBeBuild.Count > 0)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            BuildConfiguration bc = _toBeBuild[0];

            if (_resetTarget)
            {
                //If the current target is as same as initial target.
                if (bc.target.Equals(_initBuildTarget))
                {
                    //If there are other build targets
                    if (_toBeBuild.Count(build => build.target != _initBuildTarget) > 0)
                    {
                        //Add current item to the end of the list and skip to next item.
                        _toBeBuild.Add(bc);
                        _toBeBuild.RemoveAt(0);
                        continue;
                    }
                }
            }

            if (!bc.enabled) { Debug.Log(bc.name + " " + bc.uniqueId + " is Disabled. Skipped."); _toBeBuild.RemoveAt(0); }
            else if (bc.scenes.Count == 0) { Debug.Log(bc.name + " " + bc.uniqueId + " has no Scenes added. Skipped."); _toBeBuild.RemoveAt(0); }
            else
            {
                Debug.Log(bc.name + " Started...");
                sw.Start();

                string extension = String.Empty;
                switch (bc.target)
                {
                    case BuildTarget.Android:
                        extension = ".apk";
                        break;
                    case BuildTarget.BlackBerry:
                        //Not implemented.
                        break;
                    case BuildTarget.FlashPlayer:
                        //Not implemented. flv?
                        break;
                    case BuildTarget.MetroPlayer:
                        //Not implemented.
                        break;
                    case BuildTarget.NaCl:
                        //Not implemented.
                        break;
                    case BuildTarget.PS3:
                        //Not implemented.
                        break;
                    case BuildTarget.PS4:
                        //Not implemented.
                        break;
                    case BuildTarget.PSM:
                        //Not implemented.
                        break;
                    case BuildTarget.PSP2:
                        //Not implemented.
                        break;
                    case BuildTarget.SamsungTV:
                        //Not implemented. apk?
                        break;
                    case BuildTarget.StandaloneGLESEmu:
                        //Not implemented.
                        break;
                    case BuildTarget.StandaloneLinux:
                        //Not implemented.
                        break;
                    case BuildTarget.StandaloneLinux64:
                        //Not implemented.
                        break;
                    case BuildTarget.StandaloneLinuxUniversal:
                        //Not implemented.
                        break;
                    case BuildTarget.StandaloneOSXIntel:
                        //Not implemented. app? dmg?
                        break;
                    case BuildTarget.StandaloneOSXIntel64:
                        //Not implemented. app? dmg?
                        break;
                    case BuildTarget.StandaloneOSXUniversal:
                        //Not implemented. app? dmg?
                        break;
                    case BuildTarget.StandaloneWindows:
                        extension = ".exe";
                        break;
                    case BuildTarget.StandaloneWindows64:
                        extension = ".exe";
                        break;
                    case BuildTarget.Tizen:
                        //Not implemented.
                        break;
                    case BuildTarget.WP8Player:
                        //Not implemented. xap?
                        break;
                    case BuildTarget.WebPlayer:
                        extension = "";
                        break;
                    case BuildTarget.WebPlayerStreamed:
                        //Not implemented. ""?
                        break;
                    case BuildTarget.XBOX360:
                        //Not implemented.
                        break;
                    case BuildTarget.XboxOne:
                        //Not implemented.
                        break;
                    case BuildTarget.iPhone:
                        //Not implemented.
                        break;
                    default:
                        break;
                }

                //Prepare path
                string buildPath = Path.Combine(_mainBuildPath, bc.target.ToString());
                buildPath = Path.Combine(buildPath, bc.name.ToString() + " " + _timeStamp);
                if (!Directory.Exists(buildPath)) Directory.CreateDirectory(buildPath);
                buildPath = Path.Combine(buildPath, bc.name.ToString() + "-" + bc.uniqueId + extension);

                Debug.Log("Preparing scenes...");
                //Store scene paths
                string[] scenePaths = new string[bc.scenes.Count];
                for (int p = 0; p < bc.scenes.Count; p++)
                {
                    scenePaths[p] = bc.scenes[p].path;
                }

                Debug.Log("Building...");
                Debug.Log("Path : " + buildPath);
                BuildPipeline.BuildPlayer(scenePaths, buildPath, bc.target, bc.options);

                Debug.Log(bc.name + " Done. (" + sw.ElapsedMilliseconds + "ms)");
                sw.Stop();
                sw.Reset();
                _toBeBuild.RemoveAt(0);
            }
        }

        //Switch to initial target if needed.
        if (_resetTarget)
        {
            if (!EditorUserBuildSettings.activeBuildTarget.Equals(_initBuildTarget)) EditorUserBuildSettings.SwitchActiveBuildTarget(_initBuildTarget);
        }
        _toBeBuild.Clear();
        isBuilding = false;
        delayEnd = Time.realtimeSinceStartup + 5;
    }

}
