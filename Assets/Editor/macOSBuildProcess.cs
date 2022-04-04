#if UNITY_OSX
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine; 
using UnityEditor.iOS.Xcode;

public class macOSBuildProcess : IPostprocessBuildWithReport
{  
    public int callbackOrder { get; } = 999;
    public void OnPostprocessBuild(BuildReport report) {
        if (report.summary.platform == BuildTarget.StandaloneOSX) {
            var path = report.summary.outputPath;

            void Replace(string file) {
                var texts = File.ReadAllText(file);
                texts = texts.Replace("弹幕姬", "BliveAssist");
                File.WriteAllText(file, texts);
            }
 
            Replace(path + $"/macOS.xcodeproj/project.pbxproj");
            Replace(path + $"/macOS.xcodeproj/xcshareddata/xcschemes/弹幕姬.xcscheme");
            File.Move(path + $"/macOS.xcodeproj/xcshareddata/xcschemes/弹幕姬.xcscheme", path + $"/macOS.xcodeproj/xcshareddata/xcschemes/BliveAssist.xcscheme");
            Directory.Move(path + "/弹幕姬", path + "/BliveAssist");
            
            string plistPath = path + "/BliveAssist/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            PlistElementDict root = plist.root;
            root.SetString("CFBundleName", "弹幕姬");
            root.SetString("CFBundleExecutable", "BliveAssist"); 

            plist.WriteToFile(plistPath);
        }
    } 
}
#endif