Builder
=======

###Unity BuildPipeline Automation Tool

+ Builder is a BuildPipeline automation tool for Unity3D. 
+ Builder requires Unity 5 or UnityPro for any version below Unity 5 to function. [(More info)] (http://docs.unity3d.com/ScriptReference/BuildPipeline.html)
+ You are **encouraged** to contribute to the project!
+ Tested on Windows 7,8 and 10. It should also work on other systems in theory ^^

Builder uses following scripts from other Authors to implement some features;

+ [**MiniJSON** by Calvin Rien] (https://gist.github.com/darktable/1411710)
+ [**CommandLineCustomArguments** by EpixCode] (https://github.com/EpixCode/CommandLineCustomArguments)

![Builder Expanded](http://i.imgur.com/XHMJcMJ.png)

![Builder Collapsed](http://i.imgur.com/1Z29VgM.png)

###UI Cheat Sheet
+ **Save** : Overwrites current configuration
+ **Load** : Opens file dialogue to load a new configuration.
+ **Save as Default** : Saves current configuration as default configuration.
+ **Save as** : Well, pretty self explanatory isn't it :)
+ **Load Default** : Loads default configuration.
+ **Refresh Scenes** : Loads scene information defined in Build Settings.
+ **Reset Settings** : Clears current configuration, Resets default configuration and tool settings to their initial states.


###Using Builder with Command Line

1. Import Builder into your project.
2. Define a new Configuration using Builder UI.
3. Save your configuration using **Save as** button in Builder UI.
4. Now you can use saved configuration to call Builder from command line.

 **Sample code**
`"C:\Program Files (x86)\Unity\Editor\Unity.exe" -quit -batchmode -projectPath "<projectPath>" -executeMethod Builder.CommandLineBuild -CustomArgs:confPath="<Configuration File Path>"`
