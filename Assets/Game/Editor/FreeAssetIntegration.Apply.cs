using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GordonMann.EditorTools
{
    public static partial class FreeAssetIntegration
    {
        const string Vendor = "Assets/Game/ThirdParty/";
        const string Generated = "Assets/Game/Art/FreeAssets/";
        static readonly Color Paper = new Color32(249, 234, 211, 255);
        static readonly Color Teal = new Color32(46, 184, 173, 255);
        static readonly Color Slate = new Color32(55, 72, 94, 255);
        static readonly Color Dark = new Color32(24, 35, 53, 255);
        static Font Comic => AssetDatabase.LoadAssetAtPath<Font>(Vendor+"Bangers/Bangers-Regular.ttf");
        static Font Body => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();

        [MenuItem("Tools/Gordon Mann/Apply Free Asset Dressing")]
        public static void Apply()
        {
            Directory.CreateDirectory(Generated);
            AssetDatabase.Refresh();
            ConfigureImports();
            CreateSlideIcon();
            ConfigureChoicePrefab();
            Directory.CreateDirectory(Work+"/integrated");
            var report=new List<string>();
            foreach(var entry in VaultJobBlockout.VaultSceneList(VaultJobBlockout.BlockoutScenes))
            {
                var scene=EditorSceneManager.OpenScene(entry.path);
                var previous=scene.GetRootGameObjects().FirstOrDefault(r=>r.name=="Free asset dressing");
                if(previous!=null)Object.DestroyImmediate(previous);
                var root=new GameObject("Free asset dressing").transform;
                var name=scene.name;
                if(name is "MainMenu" or "Choice_1" or "Fail_Pogo" or "Fail_Pizza" or "Fail_Cannon") ExteriorDressing(root);
                else if(name is "Choice_2" or "Fail_Statue" or "Fail_Chair" or "Fail_Alarm") LobbyDressing(root);
                else if(name is "Choice_3" or "Fail_PIN" or "Fail_Knock") SecurityDressing(root);
                else if(name is "Choice_4" or "Fail_Cart" or "Fail_Unplug" or "Fail_Selfie" or "Fail_Frozen") LaserDressing(root);
                else GetawayDressing(root);
                ConfigureUI(scene);
                ConfigureEffectsAndSound(scene);
                foreach(var light in All<Light>(scene).Where(l=>l.type==LightType.Directional))
                {
                    // Neutral illumination keeps imported colours readable alongside Gordon's textures.
                    light.color=new Color(1f,.94f,.84f);
                }
                Validate(scene,report);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new Exception("Could not save "+entry.path);
                if(name.StartsWith("Choice_")||name=="MainMenu")Capture(scene,Work+"/integrated/"+name+".png");
            }
            AssetDatabase.SaveAssets();
            File.WriteAllLines(Work+"/integration-validation.txt",report);
            EditorSceneManager.OpenScene(VaultJobBlockout.BlockoutScenes+"/MainMenu.unity");
            Debug.Log("FREE_ASSETS_APPLIED: all 20 scenes updated and validated.");
        }

        static void ConfigureImports()
        {
            foreach(var file in Directory.GetFiles(Vendor,"*.png",SearchOption.AllDirectories))
            {
                string path=file.Replace('\\','/');
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                bool icon=path.Contains("/GameIcons/")||path.Contains("/input-prompts/");
                bool transparency=icon||path.Contains("particle-pack");
                bool mipmap=!icon&&!path.Contains("particle-pack");
                if(importer.textureType==(icon?TextureImporterType.Sprite:TextureImporterType.Default)&&
                    importer.alphaIsTransparency==transparency&&importer.mipmapEnabled==mipmap&&
                    importer.maxTextureSize==1024&&importer.textureCompression==TextureImporterCompression.Uncompressed&&
                    (!icon||importer.spriteImportMode==SpriteImportMode.Single))continue;
                importer.textureType=icon?TextureImporterType.Sprite:TextureImporterType.Default;
                if(icon){importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;}
                importer.alphaIsTransparency=icon||path.Contains("particle-pack");
                importer.mipmapEnabled=!icon&&!path.Contains("particle-pack");
                importer.maxTextureSize=1024;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            foreach(var file in Directory.GetFiles(Vendor,"*.ogg",SearchOption.AllDirectories))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
                if(importer.forceToMono&&importer.defaultSampleSettings.loadType==AudioClipLoadType.DecompressOnLoad&&
                    importer.defaultSampleSettings.compressionFormat==AudioCompressionFormat.Vorbis&&Mathf.Approximately(importer.defaultSampleSettings.quality,.8f))continue;
                importer.forceToMono=true;
                var settings=importer.defaultSampleSettings;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.Vorbis;
                settings.quality=.8f;
                importer.defaultSampleSettings=settings;
                importer.SaveAndReimport();
            }
        }

        static void ConfigureChoicePrefab()
        {
            const string path="Assets/Game/Prefabs/ChoiceButton.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(path);
            ThemeButton(prefab.GetComponent<Button>());
            var choice=prefab.GetComponent<ChoiceButton>();
            choice.icon.preserveAspect=true;
            choice.icon.color=Paper;
            choice.label.font=Comic;choice.label.fontStyle=FontStyle.Normal;choice.label.fontSize=34;
            choice.label.color=Paper;
            PrefabUtility.SaveAsPrefabAsset(prefab,path);
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        static void ConfigureUI(Scene scene)
        {
            foreach(var text in All<Text>(scene))
            {
                if(text.name is "Title" or "Heading" or "Message")
                {
                    text.font=Comic;text.fontStyle=FontStyle.Normal;text.color=Paper;
                    text.fontSize=text.name=="Title"?160:text.name=="Heading"?70:64;
                }
            }
            foreach(var button in All<Button>(scene))ThemeButton(button);
            foreach(var director in All<SceneDirector>(scene))
            {
                foreach(var end in new[]{director.failPanel,director.winPanel}.Where(p=>p!=null))
                {
                    var panelRect=(RectTransform)end.transform;
                    panelRect.sizeDelta=new Vector2(panelRect.sizeDelta.x,260);
                    var message=end.transform.Find("Message") as RectTransform;
                    if(message!=null){message.anchoredPosition=new Vector2(message.anchoredPosition.x,210);message.sizeDelta=new Vector2(message.sizeDelta.x,80);}
                    var sub=end.transform.Find("Sub") as RectTransform;
                    if(sub!=null){sub.anchoredPosition=new Vector2(sub.anchoredPosition.x,126);sub.sizeDelta=new Vector2(sub.sizeDelta.x,78);}
                }
                foreach(var choice in director.choices)
                {
                    var slug=new string(choice.label.Where(char.IsLetterOrDigit).ToArray());
                    choice.icon=AssetDatabase.LoadAssetAtPath<Sprite>((choice.label=="Floor Slide"?Generated:Vendor+"GameIcons/")+slug+".png");
                    if(choice.icon==null)throw new Exception("Missing choice icon for "+choice.label);
                }
            }
            foreach(var panel in All<ChoicePanel>(scene))
            {
                var old=panel.transform.Find("Control hints");if(old!=null)Object.DestroyImmediate(old.gameObject);
                var hints=UI("Control hints",panel.transform,new Vector2(.5f,0),new Vector2(0,196),new Vector2(700,38));
                var hint=hints.AddComponent<ControlHints>();
                hint.arrows=Sprite("keyboard_arrows");hint.enter=Sprite("keyboard_enter");
                hint.dpad=Sprite("xbox_dpad");hint.buttonSouth=Sprite("xbox_button_a");
                hint.navigate=Image(hints.transform,"Navigate icon",new Vector2(-260,0),new Vector2(44,44));
                var nav=Text(hints.transform,"Navigate label","Navigate",22,new Vector2(-180,0),new Vector2(110,34));nav.color=new Color(.8f,.85f,.87f);
                hint.confirm=Image(hints.transform,"Confirm icon",new Vector2(-87,0),new Vector2(44,44));
                hint.instruction=Text(hints.transform,"Instructions","Choose / click a choice",22,new Vector2(105,0),new Vector2(330,34));hint.instruction.color=nav.color;
            }
            if(scene.name=="MainMenu")MakeCredits(scene);
        }

        static void ThemeButton(Button button)
        {
            var image=button.targetGraphic as Image;
            if(image!=null)image.color=Color.white;
            var colors=button.colors;
            colors.normalColor=Dark;colors.highlightedColor=new Color32(40,91,106,255);colors.selectedColor=new Color32(40,91,106,255);colors.pressedColor=new Color32(18,58,70,255);colors.colorMultiplier=1;
            button.colors=colors;
            foreach(var text in button.GetComponentsInChildren<Text>(true))text.color=Paper;
            var feedback=button.GetComponent<InterfaceFeedback>()??button.gameObject.AddComponent<InterfaceFeedback>();
            feedback.focus=Clip("interface-sounds","select_001");feedback.activate=Clip("interface-sounds","click_003");
        }

        static void MakeCredits(Scene scene)
        {
            var canvas=All<Canvas>(scene).First(c=>c.name=="Canvas");
            var controls=canvas.transform.Find("Title menu controls");
            var gallery=All<OutcomeGallery>(scene).First();
            var previous=All<AssetCredits>(scene).FirstOrDefault();
            if(previous!=null)Object.DestroyImmediate(previous.gameObject);
            var previousOpen=controls.Find("Credits");if(previousOpen!=null)Object.DestroyImmediate(previousOpen.gameObject);
            var rowNames=new[]{"Outcomes","Sound"};
            for(int i=0;i<rowNames.Length;i++)
            {
                var r=(RectTransform)controls.Find(rowNames[i]);
                r.anchoredPosition=new Vector2(-220+i*220,-165);r.sizeDelta=new Vector2(200,64);
                var label=r.GetComponentInChildren<Text>();label.fontSize=29;label.text=rowNames[i].ToUpperInvariant();
            }
            var open=Button(controls,"Credits","CREDITS",new Vector2(220,-165),new Vector2(200,64));
            var go=UI("Asset credits",gallery.gallery.transform.parent,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1200,660));
            go.AddComponent<Image>().color=new Color(Dark.r,Dark.g,Dark.b,.99f);
            go.AddComponent<EpisodeModalLayout>();
            var credits=go.AddComponent<AssetCredits>();credits.titleControls=controls.gameObject;credits.open=open;
            var title=Text(go.transform,"Heading","ASSET CREDITS",56,new Vector2(0,270),new Vector2(1100,80));title.font=Comic;
            string content="KENNEY  /  kenney.nl\nFurniture Kit, City Kit (Roads & Commercial), Particle Pack,\nImpact Sounds, Sci-fi Sounds, Interface Sounds, Input Prompts\nCreative Commons Zero (CC0)\n\nKAY LOUSBERG  /  kaylousberg.itch.io\nFurniture Bits - Free edition  /  CC0\n\nGAME-ICONS.NET  /  Delapouite, Lorc & sbed\nChoice icons  /  Creative Commons Attribution 3.0\n\nTHE BANGERS PROJECT AUTHORS\nBangers typeface  /  SIL Open Font License 1.1\n\nFull source links and license notices accompany the game.";
            Text(go.transform,"Credits text",content,23,Vector2.zero,new Vector2(1100,490));
            credits.back=Button(go.transform,"Back to title","BACK",new Vector2(0,-280),new Vector2(290,56));
            UnityEventTools.AddPersistentListener(open.onClick,credits.Show);
            UnityEventTools.AddPersistentListener(credits.back.onClick,credits.Close);
            go.SetActive(false);
        }

        static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Vendor+"Kenney/input-prompts/Icons/"+name+".png");
        static AudioClip Clip(string pack,string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Vendor+"Kenney/"+pack+"/Audio/"+name+".ogg");
        static GameObject UI(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=position;rt.sizeDelta=size;return go;
        }
        static Text Text(Transform parent,string name,string value,int size,Vector2 position,Vector2 box)
        {
            var go=UI(name,parent,new Vector2(.5f,.5f),position,box);var text=go.AddComponent<Text>();text.font=Body;text.fontSize=size;text.text=value;text.color=Paper;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
        }
        static Image Image(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var image=UI(name,parent,new Vector2(.5f,.5f),position,size).AddComponent<Image>();image.color=Paper;image.preserveAspect=true;image.raycastTarget=false;return image;
        }
        static Button Button(Transform parent,string name,string value,Vector2 position,Vector2 size)
        {
            var go=UI(name,parent,new Vector2(.5f,.5f),position,size);var image=go.AddComponent<Image>();image.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");image.type=UnityEngine.UI.Image.Type.Sliced;var button=go.AddComponent<Button>();button.targetGraphic=image;Text(go.transform,"Label",value,29,Vector2.zero,size-Vector2.one*8);ThemeButton(button);return button;
        }

        static GameObject Model(Transform parent,string vendor,string model,Vector3 bottom,float height,float rotation=0)
        {
            string path=vendor=="KayKit"?Vendor+"KayKit/FurnitureBits/Models/"+model+".fbx":Vendor+"Kenney/"+vendor+"/Models/"+model+".fbx";
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset==null)throw new Exception("Missing model "+path);
            var wrapper=new GameObject(model).transform;wrapper.SetParent(parent,false);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,wrapper);go.transform.localPosition=Vector3.zero;
            var renderers=go.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            if(bounds.size.y<=.0001f)throw new Exception("Empty model "+path);
            float scale=height/bounds.size.y;wrapper.localScale=Vector3.one*scale;
            // Normalise around a bottom-centred pivot before rotating or placing the model.
            go.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            wrapper.position=bottom;wrapper.rotation=Quaternion.Euler(0,rotation,0);
            foreach(var collider in go.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);
            foreach(var renderer in renderers)
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>Converted(m,vendor)).ToArray();
                renderer.shadowCastingMode=ShadowCastingMode.On;
            }
            return wrapper.gameObject;
        }
        static Material Converted(Material source,string vendor)
        {
            string label=source==null?"Default":source.name;
            string filename=vendor.Replace('/','_')+"-"+new string(label.Where(c=>char.IsLetterOrDigit(c)||c=='-').ToArray());
            string path=Generated+filename+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.name=filename;
            var color=source!=null&&source.HasProperty("_Color")?source.color:Color.white;
            var texture=source!=null&&source.HasProperty("_MainTex")?source.GetTexture("_MainTex"):null;
            if(vendor=="KayKit")texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Vendor+"KayKit/FurnitureBits/Models/furniturebits_texture.png");
            if(vendor.StartsWith("city-kit"))texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Vendor+"Kenney/"+vendor+"/Models/Textures/colormap.png");
            material.SetColor("_BaseColor",texture!=null?new Color(.68f,.78f,.84f):Color.Lerp(color,Slate,.28f));
            material.SetTexture("_BaseMap",texture);material.SetFloat("_Smoothness",.18f);AssetDatabase.CreateAsset(material,path);return material;
        }

        static void ExteriorDressing(Transform root)
        {
            Model(root,"city-kit-commercial","building-a",new Vector3(-11,0,10),8,0);
            Model(root,"city-kit-commercial","building-c",new Vector3(11,0,13),10,0);
            Model(root,"city-kit-commercial","low-detail-building-g",new Vector3(-16,0,18),12,0);
            Model(root,"city-kit-roads","road-sign-stop",new Vector3(8.3f,0,-.4f),2.5f,180);
            Model(root,"furniture-kit","pottedPlant",new Vector3(-5.6f,0,3.1f),1.4f);
            Model(root,"furniture-kit","pottedPlant",new Vector3(5.6f,0,3.1f),1.4f);
        }
        static void LobbyDressing(Transform root)
        {
            Model(root,"furniture-kit","benchCushion",new Vector3(5.4f,0,4.3f),.85f,-90);
            Model(root,"furniture-kit","pottedPlant",new Vector3(5.8f,0,5.5f),1.45f);
            Model(root,"furniture-kit","computerScreen",new Vector3(-4.25f,1.38f,4.3f),.55f,180);
            Model(root,"furniture-kit","computerKeyboard",new Vector3(-4.25f,1.38f,3.94f),.05f);
            Model(root,"furniture-kit","computerMouse",new Vector3(-3.75f,1.38f,3.94f),.045f);
            Model(root,"KayKit","cactus_medium_A",new Vector3(-5.3f,1.38f,4.24f),.40f);
            Model(root,"KayKit","cabinet_small",new Vector3(-6.1f,0,5.4f),1.2f,0);
            Model(root,"KayKit","book_set",new Vector3(-6.1f,1.2f,5.4f),.21f);
        }
        static void SecurityDressing(Transform root)
        {
            Model(root,"furniture-kit","bookcaseOpenLow",new Vector3(-4.05f,0,3.35f),1.12f,0);
            Model(root,"furniture-kit","books",new Vector3(-4.05f,1.12f,3.35f),.2f);
            Model(root,"furniture-kit","pottedPlant",new Vector3(3.8f,0,3.5f),1.3f);
        }
        static void LaserDressing(Transform root)
        {
            // Keep the entire floor-slide lane and beam gaps clear.
            Model(root,"KayKit","cabinet_small",new Vector3(-2.8f,0,7.2f),1.1f);
            Model(root,"furniture-kit","computerScreen",new Vector3(-2.8f,1.1f,7.2f),.48f,90);
        }
        static void GetawayDressing(Transform root)
        {
            Model(root,"city-kit-roads","dumpster",new Vector3(4.6f,0,5.15f),1.45f,180);
            Model(root,"city-kit-commercial","building-b",new Vector3(-13,0,13),9);
            Model(root,"city-kit-commercial","low-detail-building-d",new Vector3(12,0,15),10);
            Model(root,"furniture-kit","benchCushion",new Vector3(-.1f,0,-1.25f),.82f,180);
        }

        static Material ParticleMaterial(string texture)
        {
            string path=Generated+texture+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Vendor+"Kenney/particle-pack/Textures/"+texture+".png"));
            material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
            AssetDatabase.CreateAsset(material,path);return material;
        }
        static void Particles(Transform parent,string name,string texture,Color color,int count,bool loop=false)
        {
            if(parent==null)return;
            var previous=parent.Find(name);if(previous!=null)Object.DestroyImmediate(previous.gameObject);
            var go=new GameObject(name);go.transform.SetParent(parent,false);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.duration=.8f;main.loop=loop;main.playOnAwake=true;main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,.75f);main.startSpeed=new ParticleSystem.MinMaxCurve(.3f,.8f);main.startSize=new ParticleSystem.MinMaxCurve(.12f,.4f);main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.startColor=color;main.maxParticles=40;main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Shape;
            var emission=ps.emission;emission.rateOverTime=loop?12:0;if(!loop)emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=loop?.7f:.22f;
            var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.85f,0),new GradientAlphaKey(0,1)});fade.color=gradient;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.35f),new Keyframe(1,1.3f)));
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=ParticleMaterial(texture);renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        static void ConfigureEffectsAndSound(Scene scene)
        {
            foreach(var seq in All<PortalJarSequence>(scene))
            {Particles(seq.portal,"Portal motes","spark_01",Teal,0,true);if(seq.jar!=null)seq.portalOpen=AssetDatabase.LoadAssetAtPath<AudioClip>(Generated+"Audio/Portal opening.wav");}
            foreach(var seq in All<BankFailureSequence>(scene))
            {
                if(seq.gag==BankFailureSequence.Gag.Cannon)
                {Particles(seq.effect.transform,"Cannon smoke","smoke_04",Paper,12);seq.secondCue=Clip("sci-fi-sounds","explosionCrunch_000");}
                if(seq.gag==BankFailureSequence.Gag.Pizza)seq.punchlineCue=Clip("impact-sounds","impactWood_light_000");
            }
            foreach(var seq in All<RocketSkatesSequence>(scene))
            {Particles(seq.dust,"Crash smoke","smoke_04",Paper,12);seq.crash=AssetDatabase.LoadAssetAtPath<AudioClip>(Generated+"Audio/Barrier impact.wav");seq.ignition=AssetDatabase.LoadAssetAtPath<AudioClip>(Generated+"Audio/Rocket ignition.wav");}
            foreach(var seq in All<LobbyFailureSequence>(scene))
            {if(seq.gag==LobbyFailureSequence.Gag.Chair)Particles(seq.effect,"Fountain spray","circle_01",new Color(.5f,.85f,1),14);}
        }

        static void CreateSlideIcon()
        {
            // Original vector silhouette for the one action without a matching stock icon.
            const int n=256;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false);var pixels=new Color32[n*n];
            Vector2[] points={new Vector2(70,133),new Vector2(115,110),new Vector2(164,95),new Vector2(214,95)};
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                var p=new Vector2(x,y);bool ink=Vector2.Distance(p,new Vector2(57,159))<18;
                for(int i=0;i<points.Length-1;i++)ink|=DistanceToSegment(p,points[i],points[i+1])<10;
                ink|=DistanceToSegment(p,new Vector2(115,110),new Vector2(170,130))<9;
                ink|=DistanceToSegment(p,new Vector2(27,69),new Vector2(229,69))<4;
                pixels[y*n+x]=ink?new Color32(255,255,255,255):new Color32(0,0,0,0);
            }
            texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(Generated+"FloorSlide.png",texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(Generated+"FloorSlide.png");var importer=(TextureImporter)AssetImporter.GetAtPath(Generated+"FloorSlide.png");importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
        }
        static float DistanceToSegment(Vector2 p,Vector2 a,Vector2 b)
        {
            float t=Mathf.Clamp01(Vector2.Dot(p-a,b-a)/(b-a).sqrMagnitude);
            return Vector2.Distance(p,Vector2.Lerp(a,b,t));
        }

        static void Validate(Scene scene,List<string> report)
        {
            int missing=All<Transform>(scene).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            if(missing!=0)throw new Exception(scene.name+": missing scripts "+missing);
            foreach(var renderer in All<Renderer>(scene))foreach(var material in renderer.sharedMaterials)
                if(material==null||material.shader==null||material.shader.name.Contains("InternalErrorShader"))throw new Exception(scene.name+": invalid material on "+renderer.name);
            report.Add(scene.name+": no missing scripts/materials; "+All<Renderer>(scene).Length+" renderers.");
        }
    }
}


