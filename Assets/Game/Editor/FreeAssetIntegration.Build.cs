using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GordonMann.EditorTools
{
    public static partial class FreeAssetIntegration
    {
        public static void ApplyAndBuildVerification()
        {
            Apply();
            BuildVerification();
        }
        public static void BuildVerification()
        {
            Directory.CreateDirectory(Work);
            AuditAudio();
            string output=Work+"/verification-build-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            GordonWindowsBuild.Build(output,BuildOptions.Development);
            File.WriteAllText(Work+"/verification-build-path.txt",output);
        }
        public static void BuildFinal()
        {
            Directory.CreateDirectory(Work);
            var destination=Path.GetFullPath("Builds/Gordon Mann - Free Assets 2026-11-19");
            GordonWindowsBuild.Build(destination,BuildOptions.None);
            File.WriteAllText(Work+"/final-build-path.txt",destination);
        }
        public static void PackageCredits(string destination)
        {
            if(!File.Exists(Vendor+"CREDITS.txt"))return;
            File.Copy(Vendor+"CREDITS.txt",Path.Combine(destination,"CREDITS.txt"),true);
            var licenses=Path.Combine(destination,"Asset Licenses");Directory.CreateDirectory(licenses);
            foreach(var file in Directory.GetFiles(Vendor,"*.txt",SearchOption.AllDirectories))
            {
                var relative=file.Substring(Vendor.Length).Replace('\\','_').Replace('/','_');
                File.Copy(file,Path.Combine(licenses,relative),true);
            }
            File.Copy(Vendor+"asset-sources.json",Path.Combine(licenses,"asset-sources.json"),true);
        }
        public static void AuditAudio()
        {
            var lines=new System.Collections.Generic.List<string>();
            foreach(var id in AssetDatabase.FindAssets("t:AudioClip",new[]{Vendor,Generated}))
            {
                var path=AssetDatabase.GUIDToAssetPath(id);var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                clip.LoadAudioData();var data=new float[clip.samples*clip.channels];
                if(!clip.GetData(data,0))throw new Exception("Could not inspect "+path);
                float peak=data.Max(x=>Mathf.Abs(x));
                if(peak<=.00001f||data.Any(x=>float.IsNaN(x)||float.IsInfinity(x)))throw new Exception("Invalid or silent audio "+path);
                lines.Add(path+" | seconds="+clip.length+" | peak="+peak);
            }
            File.WriteAllLines(Work+"/audio-audit.txt",lines);
        }
    }
}
