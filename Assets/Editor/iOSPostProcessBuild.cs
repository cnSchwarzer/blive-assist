#if UNITY_IOS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.iOS.Xcode;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class iOSPostProcessBuild : IPostprocessBuildWithReport {
    public int callbackOrder => 999;

    public void OnPostprocessBuild(BuildReport report) {
        OnPostprocessBuild(report.summary.platform, report.summary.outputPath);
    }

    private void OnPostprocessBuild(BuildTarget target, string path) {
        if (target == BuildTarget.iOS) {
            string plistPath = path + "/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            PlistElementDict root = plist.root;
            if (root.values.ContainsKey("CFBundleLocalizations"))
                root.values.Remove("CFBundleLocalizations");
            var array = root.CreateArray("CFBundleLocalizations");
            array.AddString("zh_CN");

            plist.WriteToFile(plistPath);
        }
    }
}
#endif