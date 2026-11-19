using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace GordonMann.EditorTools {
 public static partial class FreeAssetIntegration {
  public static string Work => Path.GetFullPath("Logs/AssetReview");
  public static void Baseline() {
   Directory.CreateDirectory(Work+"/baseline");
   foreach(var name in new[]{"MainMenu","Choice_1","Choice_2","Choice_3","Choice_4","Choice_5"}) {
    var scene=EditorSceneManager.OpenScene("Assets/Game/Blockout/Scenes/"+name+".unity");
    File.WriteAllLines(Work+"/baseline/"+name+"-hierarchy.txt",scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Select(t=>PathOf(t)+" pos="+t.position+" scale="+t.lossyScale));
    Capture(scene,Work+"/baseline/"+name+".png");
   }
   File.WriteAllText(Work+"/baseline/done.txt","Baseline scenes captured before asset integration.");
  }
  public static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
  public static void Capture(Scene scene,string file) {
   var camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
   var canvases=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
   foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=.5f;}
   Canvas.ForceUpdateCanvases();
   var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);rt.Create();
   camera.targetTexture=rt;camera.aspect=1280f/720;camera.Render();
   var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(file,image.EncodeToPNG());
   RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
  }
 }
}
