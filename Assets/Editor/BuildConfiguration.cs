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

    //Custom defines.
    public string customDefineSymbols;
    // Build options.
    public BuildOptions options = BuildOptions.None;
    // Target platform.
    public BuildTarget target = BuildTarget.Android;
    // Target GLES setting for Android and iOS
    public TargetGlesGraphics targetGlesGraphics = TargetGlesGraphics.Automatic;
    // List of scenes.
    public List<Scene> scenes = new List<Scene>();
    //UI stuff
    public bool scenesToggle = true;
    public bool toggle = true;
    public bool enabled = true;
    public string name = "No Name";
    //UNique ID
    public string uniqueId;
}