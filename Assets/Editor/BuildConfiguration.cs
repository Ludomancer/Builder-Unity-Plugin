using System;
using System.Collections.Generic;
// Holds a single build configuration.
using UnityEditor;
[Serializable]
class BuildConfiguration
{
    public BuildConfiguration(BuildOptions options, string customDefineSymbols)
    {
        this.options = options;
        this.uniqueId = UnityEngine.Random.Range(0, 1000000).ToString("000000");
        this.customDefineSymbols = customDefineSymbols;
    }

    public BuildConfiguration(BuildTarget target, BuildOptions options)
    {
        this.target = target;
        this.options = options;
        this.uniqueId = UnityEngine.Random.Range(0, 1000000).ToString("000000");
    }

    public BuildConfiguration(BuildTarget target, BuildOptions options, string customDefineSymbols)
    {
        this.target = target;
        this.options = options;
        this.uniqueId = UnityEngine.Random.Range(0, 1000000).ToString("000000");
        this.customDefineSymbols = customDefineSymbols;
    }

    public BuildConfiguration Copy()
    {
        BuildConfiguration bc = new BuildConfiguration(target, options);
        bc.scenes = scenes;
        bc.scenesToggle = scenesToggle;
        bc.enabled = enabled;
        bc.name = name;
        bc.customDefineSymbols = customDefineSymbols;
        return bc;
    }

    public string customDefineSymbols;
    public BuildOptions options = BuildOptions.None;
    public BuildTarget target = BuildTarget.Android;
    public List<Scene> scenes = new List<Scene>();
    public bool scenesToggle = true;
    public bool toggle = true;
    public bool enabled = true;
    public string name = "No Name";
    public string uniqueId;
}