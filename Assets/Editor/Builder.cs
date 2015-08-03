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
[Serializable, InitializeOnLoad]
public class Builder : EditorWindow
{
    // How to save the configuration file.
    enum SaveMode
    {
        AsDefault,
        Overwrite,
        SaveAs,
        PreCompile,
        ProjectFolder
    }

    // How to load the configuration file.
    enum LoadMode
    {
        Default,
        AskForPath,
        Args,
        PostCompile,
        ProjectFolder
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

    // Folder to output our builds.
    static string _mainBuildPath;
    // Default GUI color
    static Color _defaultGuiColor = Color.clear;
    // Styling for missing scenes
    static GUIStyle _missingSceneFoldoutStyle;
    static GUIStyle _missingSceneStyle;
    // Styling for title label
    static GUIStyle _titleStyle;
    // Used to detect when Unity starts recompiling.
    static bool _preCompileConfSaved;
    // List of scenes
    static Scene[] _sceneList;
    // List of builds
    static List<BuildConfiguration> _builds;
    // Scrollbar position
    static Vector2 _scrollPosition;
    // Most recent configuration path.
    static string mostRecentConfiguration;
    // Most recent configuration name.
    static string mostRecentConfigurationName;
    // Current sort mode.
    static SortMode _sortMode;
    // Current sort direction.
    static SortDirection _sortDirection;
    static EditorWindow window;
    // Default builds options to use when creating a new build configuration.
    static BuildOptions _defaultBuildOptions;
    // Default define symbols to use when creating a new build configuration.
    static string _defaultDefineSymbols;
    // Should we switch back to other configuration when the queue is completed.
    static bool _resetTarget;
    // Which build target we should switch to at the end.
    static BuildTarget _switchBackTo;
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

    //ctor called when Unity is loaded.
    static Builder()
    {
        //Ignore init if this is a recompile sicne we handle it differently.
        string dirPath = Path.Combine(GetDefaultDirPath(), "PreCompile.ini");
        if (!File.Exists(dirPath))
        {
            EditorApplication.update += DelayedConstructor;
        }
        EditorApplication.update += EditorUpdate;

    }

    private static void DelayedConstructor()
    {
        EditorApplication.update -= DelayedConstructor;
        //Only load if the Builder is already opened.
        if (IsAlreadyOpened())
        {
            if (_builds != null) _builds.Clear();
            _preCompileConfSaved = false;
            LoadToolSettings();
            LoadSettings(LoadMode.ProjectFolder);
            //Load scenes
            RefreshSceneList();
            window = GetWindowWithoutFocus();
        }
    }

    [MenuItem("Window/Builder")]
    static void ShowWindow()
    {
        if (!window)
        {
            if (_builds != null) _builds.Clear();

            //Load settings and default configuration
            LoadToolSettings();
            LoadSettings(LoadMode.ProjectFolder);

            //Load Failed
            if (_builds == null)
            {
                _builds = new List<BuildConfiguration>();
                _builds.Add(new BuildConfiguration(_defaultBuildOptions, _defaultDefineSymbols));
                SaveSettings(SaveMode.AsDefault);
            }

            //Load scenes
            RefreshSceneList();

            window = GetWindow();
            window.Repaint();
        }
        else window.Focus();
    }

    /// <summary>
    /// Reload recent settings when script recompile finishes.
    /// </summary>
    [UnityEditor.Callbacks.DidReloadScripts]
    public static void OnCompileScripts()
    {
        string dirPath = Path.Combine(GetDefaultDirPath(), "PreCompile.ini");
        if (File.Exists(dirPath))
        {
            if (_builds != null) _builds.Clear();
            _preCompileConfSaved = false;
            //Order is important. Calling ToolSettings after Settings makes sure Title is updated correctly instead of PreCompile.ini.
            LoadSettings(LoadMode.PostCompile);
            LoadToolSettings();
            //Load scenes
            RefreshSceneList();
            window = GetWindowWithoutFocus();
            window.Repaint();
        }
    }

    /// <summary>
    /// Is Builder already opened.
    /// </summary>
    /// <returns>Returns true if any isntance of Builder found</returns>
    private static bool IsAlreadyOpened()
    {
        Builder[] builderWindows = Resources.FindObjectsOfTypeAll<Builder>();
        return builderWindows.Length > 0;
    }
    /// <summary>
    /// Returns active Builder window withtou changing focus if it exists.
    /// </summary>
    /// <returns>Returns active Builder window withtou changing focus if it exists.</returns>
    private static Builder GetWindowWithoutFocus()
    {
        Builder[] builderWindows = Resources.FindObjectsOfTypeAll<Builder>();
        if (builderWindows.Length > 0) return builderWindows[0];
        else return null;
    }

    /// <summary>
    /// Return Builder window, create if does not exists.
    /// </summary>
    /// <returns>Return builder window</returns>
    private static Builder GetWindow()
    {
        return (Builder)EditorWindow.GetWindow(typeof(Builder));
    }


    /// <summary>
    /// Return default app data dir path.
    /// </summary>
    private static string GetDefaultDirPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity/Nidre/Builder");
    }

    /// <summary>
    /// Used to read and process command line parameters.
    /// </summary>
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
        //Load all scenes specified in EditorBuildSettings 
        List<Scene> temp = new List<Scene>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (File.Exists(scene.path))
            {
                string sceneName = scene.path.Substring(scene.path.LastIndexOf('/') + 1);
                sceneName = sceneName.Substring(0, sceneName.Length - 6);
                Scene sceneObj = new Scene(scene.path);
                temp.Add(sceneObj);
            }
        }
        _sceneList = temp.OrderBy(scene => scene.Name).ToArray();

        //Check if there any missing scenes or if any missing scene is recovered.
        if (_builds != null)
        {
            for (int i = 0; i < _builds.Count; i++)
            {
                for (int j = 0; j < _builds[i].scenes.Count; j++)
                {
                    if (!_sceneList.Contains(_builds[i].scenes[j]))
                    {
                        Debug.LogError("Missing Scene : " + _builds[i].scenes[j].Name + "," + _builds[i].scenes[j].Path);
                        _builds[i].scenes[j].isFound = false;
                    }
                    else _builds[i].scenes[j].isFound = true;
                }
            }
        }
    }

    /// <summary>
    /// Saves the configurations.
    /// </summary>
    /// <param name="saveMode">Save Mode.</param>
    static void SaveSettings(SaveMode saveMode)
    {
        Dictionary<string, object> settings = new Dictionary<string, object>();
        settings.Add("mainBuildPath", _mainBuildPath);
        settings.Add("buildCount", _builds.Count);
        settings.Add("sortBy", _sortMode);
        settings.Add("sortDirection", _sortDirection);
        settings.Add("resetTarget", _resetTarget);
        settings.Add("switchBackTo", _switchBackTo);
        settings.Add("defaultBuildOptions", _defaultBuildOptions);
        settings.Add("defaultDefineSymbols", _defaultDefineSymbols);

        Dictionary<string, object> build;
        for (int i = 0; i < _builds.Count; i++)
        {
            BuildConfiguration bc = _builds[i];
            build = new Dictionary<string, object>();
            build.Add("name", bc.name);

            string[] scenePaths = new string[bc.scenes.Count];
            for (int p = 0; p < bc.scenes.Count; p++)
            {
                scenePaths[p] = bc.scenes[p].Path;
            }
            build.Add("androidKeyStoreAlias", bc.androidKeyStoreAlias);
            build.Add("androidKeyStorePath", bc.androidKeyStorePath);
            build.Add("scenes", scenePaths);
            build.Add("target", bc.target);
            build.Add("options", bc.options);
            build.Add("scenesToggle", bc.scenesToggle);
            build.Add("toggle", bc.toggle);
            build.Add("enabled", bc.enabled);
            build.Add("uniqueId", bc.uniqueId);
            build.Add("customDefineSymbols", bc.customDefineSymbols);
            build.Add("targetGlesGraphics", bc.targetGlesGraphics);
            settings.Add("build" + i, build);
        }
        string savePath = string.Empty;
        string[] appPath;
        switch (saveMode)
        {
            case SaveMode.AsDefault:
                savePath = Path.Combine(GetDefaultDirPath(), "Default.ini");
                mostRecentConfiguration = savePath;
                mostRecentConfigurationName = Path.GetFileName(mostRecentConfiguration);
                break;
            case SaveMode.Overwrite:
                savePath = mostRecentConfiguration;
                break;
            case SaveMode.SaveAs:
                appPath = Application.dataPath.Split("/"[0]);
                string temp = EditorUtility.SaveFilePanel("Save Configuration", GetDefaultDirPath(), appPath[appPath.Length - 2], "ini");
                if (!string.IsNullOrEmpty(temp))
                {
                    savePath = temp;
                    mostRecentConfiguration = temp;
                    mostRecentConfigurationName = Path.GetFileName(mostRecentConfiguration);
                }
                else return;
                break;
            case SaveMode.PreCompile:
                savePath = Path.Combine(GetDefaultDirPath(), "PreCompile.ini");
                break;
            case SaveMode.ProjectFolder:
                appPath = Application.dataPath.Split("/"[0]);
                savePath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/")), appPath[appPath.Length - 2] + "-Nidre.Builder.ini");
                mostRecentConfiguration = savePath;
                mostRecentConfigurationName = Path.GetFileName(mostRecentConfiguration);
                break;
        }
        appPath = null;

        if (!string.IsNullOrEmpty(savePath))
        {
            using (StreamWriter writer = new StreamWriter(savePath))
            {
                writer.WriteLine(MiniJSON.Json.Serialize(settings));
            }
        }
        else
        {
            if (!EditorUtility.DisplayDialog("Can't save configuration!", "Invalid file path", "Cancel", "Retry"))
            {
                SaveSettings(saveMode);
            }
        }

    }

    /// <summary>
    /// Loads the configurations.
    /// </summary>
    /// <param name="loadMode">Load Mode.</param>
    static void LoadSettings(LoadMode loadMode)
    {
        string filePath = null;
        string serialized = null;
        switch (loadMode)
        {
            case LoadMode.Default:
                string dirPath = GetDefaultDirPath();
                filePath = Path.Combine(dirPath, "Default.ini");
                // Create initial folder structure
                if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
                // Save default settings
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Close();
                    _mainBuildPath = null;
                    _builds = new List<BuildConfiguration>();
                    _builds.Add(new BuildConfiguration(_defaultBuildOptions, _defaultDefineSymbols));
                    SaveSettings(SaveMode.AsDefault);
                }
                serialized = System.IO.File.ReadAllText(filePath);
                break;
            case LoadMode.AskForPath:
                filePath = EditorUtility.OpenFilePanel("Load Configuration", GetDefaultDirPath(), "ini");
                if (!File.Exists(filePath))
                {
                    return;
                }
                serialized = System.IO.File.ReadAllText(filePath);
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
                serialized = System.IO.File.ReadAllText(filePath);
                break;
            case LoadMode.PostCompile:
                filePath = Path.Combine(GetDefaultDirPath(), "PreCompile.ini");
                serialized = System.IO.File.ReadAllText(filePath);
                File.Delete(filePath);
                break;
            case LoadMode.ProjectFolder:
                string[] appPath = Application.dataPath.Split("/"[0]);
                filePath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/")), appPath[appPath.Length - 2] + "-Nidre.Builder.ini");
                if (!!string.IsNullOrEmpty(filePath)) serialized = System.IO.File.ReadAllText(filePath);
                break;
        }

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

                if (deserialized.ContainsKey("defaultBuildOptions"))
                {
                    _defaultBuildOptions = (BuildOptions)Enum.Parse(typeof(BuildOptions), deserialized["defaultBuildOptions"] as string);
                }

                if (deserialized.ContainsKey("defaultDefineSymbols"))
                {
                    _defaultDefineSymbols = deserialized["defaultDefineSymbols"] as string;
                }

                if (deserialized.ContainsKey("switchBackTo"))
                {
                    _switchBackTo = (BuildTarget)Enum.Parse(typeof(BuildTarget), deserialized["switchBackTo"] as string);
                }
                else
                {
                    _switchBackTo = EditorUserBuildSettings.activeBuildTarget;
                }

                for (int i = 0; i < _builds.Capacity; i++)
                {
                    BuildConfiguration bc = new BuildConfiguration(_defaultBuildOptions, _defaultDefineSymbols);
                    Dictionary<string, object> buildConf = deserialized["build" + i] as Dictionary<string, object>;
                    List<object> temp = buildConf["scenes"] as List<object>;
                    bc.scenes = new List<Scene>();
                    foreach (string scenePath in temp)
                    {
                        Scene newScene = new Scene(scenePath);
                        bc.scenes.Add(newScene);
                    }
                    bc.name = buildConf["name"] as string;
                    bc.options = (BuildOptions)Enum.Parse(typeof(BuildOptions), buildConf["options"] as string);
                    bc.target = (BuildTarget)Enum.Parse(typeof(BuildTarget), buildConf["target"] as string);
                    if (buildConf.ContainsKey("androidKeyStoreAlias")) bc.androidKeyStoreAlias = buildConf["androidKeyStoreAlias"] as string;
                    if (buildConf.ContainsKey("androidKeyStorePath")) bc.androidKeyStorePath = buildConf["androidKeyStorePath"] as string;
                    bc.scenesToggle = (bool)buildConf["scenesToggle"];
                    bc.toggle = (bool)buildConf["toggle"];
                    bc.enabled = (bool)buildConf["enabled"];
                    bc.uniqueId = int.Parse(buildConf["uniqueId"] as string).ToString("000000");
                    if (buildConf.ContainsKey("customDefineSymbols")) bc.customDefineSymbols = buildConf["customDefineSymbols"] as string;
                    if (buildConf.ContainsKey("targetGlesGraphics")) bc.targetGlesGraphics = (TargetGlesGraphics)Enum.Parse(typeof(TargetGlesGraphics), buildConf["targetGlesGraphics"] as string);
                    _builds.Add(bc);
                }
                ApplySort();
                serialized = null;
                mostRecentConfiguration = filePath;
                mostRecentConfigurationName = Path.GetFileName(mostRecentConfiguration);
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
                                _builds.Add(new BuildConfiguration(_defaultBuildOptions, _defaultDefineSymbols));
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
                        case LoadMode.ProjectFolder:
                            LoadSettings(LoadMode.Default);
                            break;
                    }
                }
                Debug.LogException(e);
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
    static void SaveToolSettings(bool isPreCompile)
    {
        string path = Path.Combine(GetDefaultDirPath(), "Settings.ini");

        Dictionary<string, object> settings = new Dictionary<string, object>();

        settings.Add("sortUtilsToggle", _sortUtilsToggle);
        settings.Add("toolOptionsToggle", _toolOptionsToggle);
        settings.Add("listUtilsToggle", _listUtilsToggle);
        if (isPreCompile) settings.Add("mostRecentConfiguration", mostRecentConfiguration);

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
        string dirPath = GetDefaultDirPath();
        string filePath = Path.Combine(dirPath, "Settings.ini");

        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close();
            SaveToolSettings(false);
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
                _sortUtilsToggle = (bool)deserialized["sortUtilsToggle"];
                _toolOptionsToggle = (bool)deserialized["toolOptionsToggle"];
                _listUtilsToggle = (bool)deserialized["listUtilsToggle"];
                if (deserialized.ContainsKey("mostRecentConfiguration"))
                {
                    mostRecentConfiguration = deserialized["mostRecentConfiguration"] as string;
                    if (!string.IsNullOrEmpty(mostRecentConfiguration)) mostRecentConfigurationName = Path.GetFileName(mostRecentConfiguration);
                }
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
                    SaveToolSettings(false);
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
        _builds.Add(new BuildConfiguration(_defaultBuildOptions, _defaultDefineSymbols));
        SaveSettings(SaveMode.AsDefault);
        _defaultBuildOptions = BuildOptions.None;
        SaveToolSettings(false);
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
    /// Generate Unique Id for Build Configuration
    /// </summary>
    static string GenerateUniqueId()
    {
        string id = UnityEngine.Random.Range(0, 1000000).ToString("000000");
        int index = 0;
        while (index < _builds.Count - 1)
        {
            if (_builds[index].uniqueId.Equals(id))
            {
                id = UnityEngine.Random.Range(0, 1000000).ToString("000000");
                index = 0;
            }
            else index++;
        }
        return id;
    }

    /// <summary>
    /// Iterates and build each item in build list.
    /// May reorder list if _resetTarget is true to make sure we return back to initial configration in most optimal way.
    /// </summary>
    static void Build()
    {
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
                if (bc.target.Equals(_switchBackTo))
                {
                    //If there are other build targets
                    if (_toBeBuild.Count(build => build.target != _switchBackTo) > 0)
                    {
                        //Add current item to the end of the list and skip to next item.
                        _toBeBuild.Add(bc);
                        _toBeBuild.RemoveAt(0);
                        continue;
                    }
                }
            }

            if (!bc.enabled) { Debug.LogWarning(bc.name + " " + bc.uniqueId + " is Disabled. Skipped."); _toBeBuild.RemoveAt(0); }
            else if (bc.scenes.Count == 0) { Debug.LogWarning(bc.name + " " + bc.uniqueId + " has no Scenes added. Skipped."); _toBeBuild.RemoveAt(0); }
            else if (bc.scenes.Count(scene => !scene.isFound) > 0) { Debug.LogWarning(bc.name + " " + bc.uniqueId + " has missing Scenes. Skipped."); _toBeBuild.RemoveAt(0); }
            else
            {
                Debug.Log(bc.name + " Started...");
                sw.Start();

                string extension = String.Empty;
                string defineSymbolsBackup = String.Empty;
                TargetGlesGraphics targetGlesGraphicsBackup = TargetGlesGraphics.Automatic;
                BuildTargetGroup currentBuildTargetGroup;
                bool isBuildGroupUnkown = false;
                switch (bc.target)
                {
                    case BuildTarget.Android:
                        currentBuildTargetGroup = BuildTargetGroup.Android;
                        extension = ".apk";
                        PlayerSettings.Android.keystoreName = bc.androidKeyStorePath;
                        PlayerSettings.Android.keystorePass = bc.androidKeyStorePass;
                        PlayerSettings.Android.keyaliasName = bc.androidKeyStoreAlias;
                        PlayerSettings.Android.keyaliasPass = bc.androidKeyStoreAliasPass;
                        break;
                    case BuildTarget.BlackBerry:
                        currentBuildTargetGroup = BuildTargetGroup.BlackBerry;
                        //Not implemented.
                        break;
#if UNITY_4
                    case BuildTarget.FlashPlayer:
                        currentBuildTargetGroup = BuildTargetGroup.FlashPlayer;
                        //Not implemented. flv?
                        break;
#endif
                    case BuildTarget.MetroPlayer:
                        currentBuildTargetGroup = BuildTargetGroup.Metro;
                        //Not implemented.
                        break;
#if UNITY_4_2 ||  UNITY_4_1 ||  UNITY_4_0
                    case BuildTarget.NaCl:
                        currentBuildTargetGroup = BuildTargetGroup.NaCl;
                        //Not implemented.
                        break;
#endif
                    case BuildTarget.PS3:
                        currentBuildTargetGroup = BuildTargetGroup.PS3;
                        //Not implemented.
                        break;
                    case BuildTarget.PS4:
                        currentBuildTargetGroup = BuildTargetGroup.PS4;
                        //Not implemented.
                        break;
                    case BuildTarget.PSM:
                        currentBuildTargetGroup = BuildTargetGroup.PSM;
                        //Not implemented.
                        break;
                    case BuildTarget.PSP2:
                        currentBuildTargetGroup = BuildTargetGroup.PSP2;
                        //Not implemented.
                        break;
                    case BuildTarget.SamsungTV:
                        currentBuildTargetGroup = BuildTargetGroup.SamsungTV;
                        //Not implemented. apk?
                        break;
                    case BuildTarget.StandaloneGLESEmu:
                        currentBuildTargetGroup = BuildTargetGroup.Standalone;
                        //Not implemented.
                        break;
                    case BuildTarget.StandaloneLinux:
                        currentBuildTargetGroup = BuildTargetGroup.Standalone;
                        //Not implemented.
                        break;
                    case BuildTarget.StandaloneLinux64:
                        currentBuildTargetGroup = BuildTargetGroup.Standalone;
                        //Not implemented.
                        break;
                    case BuildTarget.StandaloneLinuxUniversal:
                        currentBuildTargetGroup = BuildTargetGroup.Standalone;
                        //Not implemented.
                        break;
                    case BuildTarget.StandaloneOSXIntel:
                        currentBuildTargetGroup = BuildTargetGroup.Standalone;
                        //Not implemented. app? dmg?
                        break;
                    case BuildTarget.StandaloneOSXIntel64:
                        currentBuildTargetGroup = BuildTargetGroup.Standalone;
                        //Not implemented. app? dmg?
                        break;
                    case BuildTarget.StandaloneOSXUniversal:
                        currentBuildTargetGroup = BuildTargetGroup.Standalone;
                        //Not implemented. app? dmg?
                        break;
                    case BuildTarget.StandaloneWindows:
                        currentBuildTargetGroup = BuildTargetGroup.Standalone;
                        extension = ".exe";
                        break;
                    case BuildTarget.StandaloneWindows64:
                        currentBuildTargetGroup = BuildTargetGroup.Standalone;
                        extension = ".exe";
                        break;
                    case BuildTarget.Tizen:
                        currentBuildTargetGroup = BuildTargetGroup.Tizen;
                        //Not implemented.
                        break;
                    case BuildTarget.WP8Player:
                        currentBuildTargetGroup = BuildTargetGroup.WP8;
                        //Not implemented. xap?
                        break;
                    case BuildTarget.WebPlayer:
                        currentBuildTargetGroup = BuildTargetGroup.WebPlayer;
                        extension = "";
                        break;
                    case BuildTarget.WebPlayerStreamed:
                        currentBuildTargetGroup = BuildTargetGroup.WebPlayer;
                        //Not implemented. ""?
                        break;
                    case BuildTarget.XBOX360:
                        currentBuildTargetGroup = BuildTargetGroup.XBOX360;
                        //Not implemented.
                        break;
                    case BuildTarget.XboxOne:
                        currentBuildTargetGroup = BuildTargetGroup.XboxOne;
                        //Not implemented.
                        break;
                    case BuildTarget.iOS:
                        currentBuildTargetGroup = BuildTargetGroup.iOS;
                        //Not implemented.
                        break;
                    default:
                        //Make compiler happy. We can't have it undefined so we just but a dummy define in default which is not likely to be fired as long as
                        //this switch case is maintained with new platforms added. 
                        currentBuildTargetGroup = BuildTargetGroup.Android;
                        isBuildGroupUnkown = true;
                        break;
                }

                //Ignore if we have somehow triggered default case of above switch.
                if (!isBuildGroupUnkown)
                {
                    //Change Build Symbols
                    defineSymbolsBackup = PlayerSettings.GetScriptingDefineSymbolsForGroup(currentBuildTargetGroup);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(currentBuildTargetGroup, bc.customDefineSymbols);

                    //Change target GLES Graphics
                    targetGlesGraphicsBackup = PlayerSettings.targetGlesGraphics;
                    PlayerSettings.targetGlesGraphics = bc.targetGlesGraphics;
                }

                //Prepare path
                string buildPath = Path.Combine(_mainBuildPath, bc.target.ToString());
                buildPath = Path.Combine(buildPath, bc.name + " " + _timeStamp);
                if (!Directory.Exists(buildPath)) Directory.CreateDirectory(buildPath);
                buildPath = Path.Combine(buildPath, bc.name + "-" + bc.uniqueId + extension);

                Debug.Log("Preparing scenes...");
                //Store scene paths
                string[] scenePaths = new string[bc.scenes.Count];
                for (int p = 0; p < bc.scenes.Count; p++)
                {
                    scenePaths[p] = bc.scenes[p].Path;
                }

                Debug.Log("Building...");
                Debug.Log("Path : " + buildPath);
                string buildMessage = BuildPipeline.BuildPlayer(scenePaths, buildPath, bc.target, bc.options);
                if (!string.IsNullOrEmpty(buildMessage))
                {
                    Debug.LogError("Build Failed : " + buildMessage);
                }

                //Ignore if we have somehow triggered default case of above switch.
                if (!isBuildGroupUnkown)
                {
                    //Set Define Symbols to it's initial state before build.
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(currentBuildTargetGroup, defineSymbolsBackup);

                    //Set Target GLES Graphics to it's initial state before build.
                    PlayerSettings.targetGlesGraphics = targetGlesGraphicsBackup;
                }

                Debug.Log(bc.name + " Done. (" + sw.ElapsedMilliseconds + "ms)");
                sw.Stop();
                sw.Reset();
                _toBeBuild.RemoveAt(0);
            }
        }

        //Switch to initial target if needed.
        if (_resetTarget)
        {
            if (!EditorUserBuildSettings.activeBuildTarget.Equals(_switchBackTo)) EditorUserBuildSettings.SwitchActiveBuildTarget(_switchBackTo);
        }
        _toBeBuild.Clear();
    }

    static void EditorUpdate()
    {
        //Check if Editor started compilation
        if (EditorApplication.isCompiling)
        {
            //Save current settings if we haven't already.
            if (!_preCompileConfSaved)
            {
                SaveToolSettings(true);
                SaveSettings(SaveMode.PreCompile);
                _preCompileConfSaved = true;
            }
        }
    }

    void OnGUI()
    {
        if (!UnityEditor.BuildPipeline.isBuildingPlayer)
        {
            if (_builds != null)
            {

                #region PrepareGuiStyles
                //Create a title style if we haven't already.
                //Calling this piece of code outside of OnGUI throws an exception. That's why we do it here.
                if (_titleStyle == null)
                {
                    _titleStyle = new GUIStyle(EditorStyles.boldLabel);
                    _titleStyle.alignment = TextAnchor.MiddleCenter;
                }
                //Create a missing scene style if we haven't already.
                if (_missingSceneStyle == null)
                {
                    _missingSceneStyle = new GUIStyle(EditorStyles.label);
                    _missingSceneStyle.focused.textColor = Color.red;
                    _missingSceneStyle.normal.textColor = Color.red;
                    _missingSceneStyle.active.textColor = Color.red;
                    _missingSceneStyle.hover.textColor = Color.red;
                    _missingSceneStyle.onFocused.textColor = Color.red;
                    _missingSceneStyle.onNormal.textColor = Color.red;
                    _missingSceneStyle.onActive.textColor = Color.red;
                    _missingSceneStyle.onHover.textColor = Color.red;
                }
                //Create a missing scene style for foldouts if we haven't already.
                if (_missingSceneFoldoutStyle == null)
                {
                    _missingSceneFoldoutStyle = new GUIStyle(EditorStyles.foldout);
                    _missingSceneFoldoutStyle.focused.textColor = Color.red;
                    _missingSceneFoldoutStyle.normal.textColor = Color.red;
                    _missingSceneFoldoutStyle.active.textColor = Color.red;
                    _missingSceneFoldoutStyle.hover.textColor = Color.red;
                    _missingSceneFoldoutStyle.onFocused.textColor = Color.red;
                    _missingSceneFoldoutStyle.onNormal.textColor = Color.red;
                    _missingSceneFoldoutStyle.onActive.textColor = Color.red;
                    _missingSceneFoldoutStyle.onHover.textColor = Color.red;
                }
                if (_defaultGuiColor.Equals(Color.clear))
                {
                    _defaultGuiColor = GUI.backgroundColor;
                }
                #endregion

                EditorGUILayout.SelectableLabel(mostRecentConfigurationName, _titleStyle, GUILayout.Height(20));

                #region BuilderUtils
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save", EditorStyles.miniButtonLeft))
                {
                    if (mostRecentConfiguration.Equals(Path.Combine(GetDefaultDirPath(), "Default.ini")))
                    {
                        if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to overwrite 'Default Configuration'?", "Save", "Cancel"))
                        {
                            SaveSettings(SaveMode.Overwrite);
                        }
                    }
                    else SaveSettings(SaveMode.Overwrite);
                }
                if (GUILayout.Button("Save As ...", EditorStyles.miniButtonMid))
                {
                    SaveSettings(SaveMode.SaveAs);
                }
                if (GUILayout.Button("Save As Project Default", EditorStyles.miniButtonMid))
                {
                    SaveSettings(SaveMode.ProjectFolder);
                }
                if (GUILayout.Button("Load", EditorStyles.miniButtonRight))
                {
                    LoadSettings(LoadMode.AskForPath);
                    RefreshSceneList();
                    Repaint();
                }
                EditorGUILayout.Space();
                if (GUILayout.Button("Save as Default", EditorStyles.miniButtonLeft))
                {
                    SaveSettings(SaveMode.AsDefault);
                }
                if (GUILayout.Button("Load Default", EditorStyles.miniButtonRight))
                {
                    LoadSettings(LoadMode.Default);
                    RefreshSceneList();
                    Repaint();
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
                EditorGUILayout.Space();
                #endregion

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                #region ToolOptions

                bool prevToggle = _toolOptionsToggle;
                _toolOptionsToggle = EditorGUILayout.Foldout(_toolOptionsToggle, "Tool Options");
                if (!prevToggle.Equals(_toolOptionsToggle)) SaveToolSettings(false);

                if (_toolOptionsToggle)
                {
                    EditorGUILayout.BeginHorizontal();
                    _resetTarget = EditorGUILayout.ToggleLeft("Switch to ", _resetTarget, GUILayout.Width(75));
                    _switchBackTo = (BuildTarget)EditorGUILayout.EnumPopup(_switchBackTo, GUILayout.MaxWidth(150));
                    EditorGUILayout.LabelField(" when completed.");
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.Space();
                #endregion

                #region SortUtils

                prevToggle = _sortUtilsToggle;
                _sortUtilsToggle = EditorGUILayout.Foldout(_sortUtilsToggle, "Sort Options");
                if (!prevToggle.Equals(_sortUtilsToggle)) SaveToolSettings(false);
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
                EditorGUILayout.Space();
                #endregion

                #region ListUtils

                prevToggle = _listUtilsToggle;
                _listUtilsToggle = EditorGUILayout.Foldout(_listUtilsToggle, "List Options");
                if (!prevToggle.Equals(_listUtilsToggle)) SaveToolSettings(false);
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
                        _builds.Add(new BuildConfiguration(_defaultBuildOptions, _defaultDefineSymbols));
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.Space();
                #endregion

                #region BuildQueue
                EditorGUILayout.LabelField("Build Queue", EditorStyles.boldLabel, null);

                isScrollAlive = true;
                for (int i = 0; i < _builds.Count; i++)
                {
                    EditorGUI.indentLevel = 0;
                    BuildConfiguration bc = _builds[i];
                    string selectedScenes = string.Empty;
                    if (bc.scenes.Count > 0)
                    {
                        selectedScenes = "(";
                        for (int s = 0; s < bc.scenes.Count; s++)
                        {
                            selectedScenes += bc.scenes[s].Name;
                            if (s < bc.scenes.Count - 1)
                            {
                                selectedScenes += ",";
                            }
                        }
                        selectedScenes += ")";
                    }

                    EditorGUILayout.BeginHorizontal();
                    #region ListItemTitle
                    bc.enabled = EditorGUILayout.ToggleLeft("", bc.enabled, GUILayout.Width(10));
                    Rect r = GUILayoutUtility.GetLastRect();
                    r.width = 65;
                    r.x += 15;
                    if (bc.scenes.Count(item => !item.isFound) > 0) bc.toggle = EditorGUI.Foldout(r, bc.toggle, bc.uniqueId, true, _missingSceneFoldoutStyle);
                    else bc.toggle = EditorGUI.Foldout(r, bc.toggle, bc.uniqueId, true);

                    GUILayout.Space(65);
                    bc.name = EditorGUILayout.TextField(bc.name);
                    bc.target = (BuildTarget)EditorGUILayout.EnumPopup("", bc.target, GUILayout.MaxWidth(150));
                    EditorGUILayout.SelectableLabel(selectedScenes);
                    GUILayout.FlexibleSpace();
                    #endregion

                    #region BuildListUtils
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("Up", EditorStyles.miniButtonLeft, GUILayout.Width(40)))
                    {
                        BuildConfiguration buildConf = _builds[i];
                        _builds.RemoveAt(i);
                        _builds.Insert(i - 1, buildConf);
                    }
                    GUI.enabled = true;
                    if (GUILayout.Button("Copy", EditorStyles.miniButtonMid, GUILayout.Width(40)))
                    {
                        BuildConfiguration buildConf = _builds[i].Copy();
                        buildConf.uniqueId = GenerateUniqueId();
                        _builds.Insert(i + 1, buildConf);
                    }
                    GUI.enabled = i < _builds.Count - 1;
                    if (GUILayout.Button("Down", EditorStyles.miniButtonRight, GUILayout.Width(40)))
                    {
                        BuildConfiguration buildConf = _builds[i];
                        _builds.RemoveAt(i);
                        _builds.Insert(i + 1, buildConf);
                    }
                    GUI.enabled = true;
                    if (GUILayout.Button("+", EditorStyles.miniButtonLeft, GUILayout.Width(25)))
                    {
                        BuildConfiguration buildConf = new BuildConfiguration(_defaultBuildOptions, _defaultDefineSymbols);
                        buildConf.uniqueId = GenerateUniqueId();
                        _builds.Insert(i + 1, buildConf);
                    }
                    GUI.enabled = _builds.Count > 1;
                    if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(25)))
                    {
                        _builds.RemoveAt(i);
                    }
                    GUI.enabled = true;
                    #endregion
                    EditorGUILayout.EndHorizontal();

                    #region ListItemDetails
                    if (bc.toggle)
                    {
                        EditorGUILayout.BeginVertical();
                        GUI.enabled = bc.enabled;
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField("Platform Specific", GUILayout.Width(125));
                        EditorGUI.indentLevel++;
                        switch (bc.target)
                        {
                            case BuildTarget.Android:
                                //Very ugly code due to Unity Editor GUI limitations. I couldn't find something cleaner. Let me know if anyone finds anything!
                                //Very hard to modularize this part per platform with Unity Editor GUI.
                                EditorGUILayout.BeginVertical();
                                EditorGUILayout.LabelField("Singing");
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Keystore File : ", GUILayout.Width(170));
                                if (string.IsNullOrEmpty(bc.androidKeyStorePath) || bc.androidKeyStorePath.Equals(@"NULL"))
                                {
                                    EditorGUILayout.LabelField("Choose Keystore", GUILayout.MaxWidth(150));
                                }
                                else
                                {
                                    EditorGUILayout.LabelField(bc.androidKeyStorePath, GUILayout.MaxWidth(window.position.width - 95 - 25 - 20));
                                }
                                if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(25))) { bc.androidKeyStorePath = EditorUtility.OpenFilePanel("Select Keystore File", bc.androidKeyStorePath, "keystore"); }
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Keystore Password : ", GUILayout.Width(200));
                                bc.androidKeyStorePass = GUILayout.PasswordField(bc.androidKeyStorePass, '*', GUILayout.MaxWidth(125));
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Keystore Alias : ", GUILayout.Width(200));
                                bc.androidKeyStoreAlias = GUILayout.TextField(bc.androidKeyStoreAlias, GUILayout.MaxWidth(125));
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Keystore Alias Password : ", GUILayout.Width(200));
                                bc.androidKeyStoreAliasPass = GUILayout.PasswordField(bc.androidKeyStoreAliasPass, '*', GUILayout.MaxWidth(125));
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.EndVertical();
                                break;
                        }
                        EditorGUI.indentLevel--;
                        EditorGUI.indentLevel--;
                        EditorGUI.indentLevel++;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Build Options", GUILayout.Width(125));
                        bc.options = (BuildOptions)EditorGUILayout.EnumMaskField("", bc.options);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Make default"))
                        {
                            _defaultBuildOptions = bc.options;
                            SaveToolSettings(false);
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Define Symbols", GUILayout.Width(125));
                        string prevSymbols = bc.customDefineSymbols;
                        bc.customDefineSymbols = EditorGUILayout.TextField("", bc.customDefineSymbols);
                        if (bc.customDefineSymbols != null && !bc.customDefineSymbols.Equals(prevSymbols))
                        {
                            bc.customDefineSymbols = bc.customDefineSymbols.Trim();
                            bc.customDefineSymbols = bc.customDefineSymbols.Trim(';');
                            bc.customDefineSymbols = bc.customDefineSymbols.Replace(" ", ";");
                        }
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Make default"))
                        {
                            _defaultDefineSymbols = bc.customDefineSymbols;
                            SaveToolSettings(false);
                        }
                        EditorGUILayout.EndHorizontal();

                        if (bc.target == BuildTarget.Android || bc.target == BuildTarget.iOS || bc.target == BuildTarget.BlackBerry)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Graphics Level", GUILayout.Width(125));
                            bc.targetGlesGraphics = (TargetGlesGraphics)EditorGUILayout.EnumPopup((Enum)bc.targetGlesGraphics);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                        }

                        #region SceneList
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.BeginHorizontal();
                        bc.scenesToggle = EditorGUILayout.Foldout(bc.scenesToggle, "Scenes " + selectedScenes);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Select All", EditorStyles.miniButtonLeft, GUILayout.Width(125)))
                        {
                            for (int k = 0; k < _sceneList.Length; k++)
                            {
                                if (!bc.scenes.Contains(_sceneList[k]))
                                {
                                    bc.scenes.Add(_sceneList[k]);
                                }
                            }
                        }
                        if (GUILayout.Button("Select None", EditorStyles.miniButtonRight, GUILayout.Width(125)))
                        {
                            for (int k = 0; k < _sceneList.Length; k++)
                            {
                                bc.scenes.Remove(_sceneList[k]);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUI.indentLevel++;
                        if (bc.scenesToggle)
                        {
                            #region IncludedScenes
                            EditorGUILayout.LabelField("Included", EditorStyles.boldLabel);
                            EditorGUI.indentLevel++;
                            for (int k = 0; k < bc.scenes.Count; k++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                if (bc.scenes[k].isFound)
                                {
                                    if (!EditorGUILayout.ToggleLeft(bc.scenes[k].Name, true))
                                    {
                                        bc.scenes.Remove(bc.scenes[k]);
                                        break;
                                    }
                                }
                                else
                                {
                                    EditorGUILayout.LabelField(bc.scenes[k].Name, _missingSceneStyle);
                                }
                                GUI.enabled = k > 0;
                                if (GUILayout.Button("Up", EditorStyles.miniButtonLeft, GUILayout.Width(40)))
                                {
                                    Scene scene;
                                    if (Event.current.control)
                                    {
                                        scene = bc.scenes[k];
                                        bc.scenes[k] = bc.scenes[0];
                                        bc.scenes[0] = scene;
                                    }
                                    else
                                    {
                                        scene = bc.scenes[k];
                                        bc.scenes[k] = bc.scenes[k - 1];
                                        bc.scenes[k - 1] = scene;
                                    }
                                }
                                GUI.enabled = k < bc.scenes.Count - 1;
                                if (GUILayout.Button("Down", EditorStyles.miniButtonRight, GUILayout.Width(40)))
                                {
                                    Scene scene = bc.scenes[k];
                                    bc.scenes[k] = bc.scenes[k + 1];
                                    bc.scenes[k + 1] = scene;
                                }
                                GUI.enabled = true;
                                if (!bc.scenes[k].isFound)
                                {
                                    if (GUILayout.Button("Locate", EditorStyles.miniButtonLeft, GUILayout.Width(75)))
                                    {
                                        string file = EditorUtility.OpenFilePanel("Locate " + bc.scenes[k].Name, Application.dataPath, "unity");
                                        file = "Assets" + file.Replace(Application.dataPath, "");
                                        if (bc.scenes.Count(scene => scene.Path == file) > 0)
                                        {
                                            bc.scenes.RemoveAt(k);
                                        }
                                        else
                                        {
                                            bc.scenes[k].Path = file;
                                            bc.scenes[k].isFound = true;
                                        }
                                    }
                                    GUI.backgroundColor = Color.red;
                                    if (GUILayout.Button("Remove", EditorStyles.miniButtonRight, GUILayout.Width(75)))
                                    {
                                        bc.scenes.RemoveAt(k);
                                    }
                                    GUI.backgroundColor = _defaultGuiColor;
                                }
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.EndHorizontal();
                            }

                            EditorGUI.indentLevel--;
                            #endregion
                            #region ExcludedScenes
                            EditorGUILayout.LabelField("Excluded", EditorStyles.boldLabel);
                            EditorGUI.indentLevel++;
                            for (int k = 0; k < _sceneList.Length; k++)
                            {
                                if (!bc.scenes.Contains(_sceneList[k]))
                                {
                                    if (EditorGUILayout.ToggleLeft(_sceneList[k].Name, false))
                                    {
                                        bc.scenes.Add(_sceneList[k]);
                                    }
                                }
                            }
                            EditorGUI.indentLevel--;
                            #endregion
                        }
                        EditorGUILayout.EndVertical();
                        #endregion
                        EditorGUILayout.EndVertical();
                        GUI.enabled = true;
                    }
                    #endregion
                }
                #endregion

                #region Build
                //Disable build button if there are no enabled builds.
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

                EditorGUILayout.BeginHorizontal();
                float buildButtonSize = 100;
                //Center build button.
                GUILayout.Space(window.position.width * 0.5f - buildButtonSize * 0.5f);
                if (GUILayout.Button("Start Build", GUILayout.Width(buildButtonSize)))
                {
                    SelectBuildPath();
                    EditorGUILayout.EndScrollView();
                    Build();
                    isScrollAlive = false;
                    return;
                }
                EditorGUILayout.EndHorizontal();

                GUI.enabled = true;
                #endregion

                if (isScrollAlive) EditorGUILayout.EndScrollView();
                if (window) EditorUtility.SetDirty(window);
            }
        }
    }
}
