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
internal class BuildConfiguration
{
    public string androidKeyStorePath = "NULL";
    public string androidKeyStorePass = "";
    public string androidKeyStoreAlias = "";
    public string androidKeyStoreAliasPass = "";
    public string customDefineSymbols;
    public BuildOptions options = BuildOptions.None;
    private BuildTarget _target = BuildTarget.Android;
    public List<Scene> scenes = new List<Scene>();
    public bool scenesToggle = true;
    public bool toggle = true;
    public bool enabled = true;
    public string name = "No Name";
    public string uniqueId;

    public BuildTarget Target
    {
        get { return _target; }
        set
        {
            _target = value;
        }
    }

    public BuildConfiguration(BuildOptions options, string customDefineSymbols)
    {
        this.options = options;
        this.customDefineSymbols = customDefineSymbols;
        this.uniqueId = UnityEngine.Random.Range(0, 1000000).ToString("000000");
    }

    public BuildConfiguration(BuildTarget target, BuildOptions options)
    {
        this.Target = target;
        this.options = options;
        this.uniqueId = UnityEngine.Random.Range(0, 1000000).ToString("000000");
    }

    public BuildConfiguration(BuildTarget target, BuildOptions options, string customDefineSymbols)
        : this(target, options)
    {
        this.customDefineSymbols = customDefineSymbols;
    }

    public BuildConfiguration Copy()
    {
        BuildConfiguration bc = new BuildConfiguration(Target, options);
        bc.scenes = scenes;
        bc.scenesToggle = scenesToggle;
        bc.enabled = enabled;
        bc.name = name;
        bc.customDefineSymbols = customDefineSymbols;
        return bc;
    }
}