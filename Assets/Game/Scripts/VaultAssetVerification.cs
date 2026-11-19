#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;

namespace GordonMann
{
    // Only compiled in the editor and development builds; normal play never starts it.
    public sealed class VaultAssetVerification : MonoBehaviour
    {
        string folder;
        readonly List<string> passes=new();
        readonly List<string> failures=new();
        readonly List<string> errors=new();
        bool persistence;
        float started;
        int shot;
        ParticleSystem[] effects=Array.Empty<ParticleSystem>();
        readonly HashSet<string> emittedEffects=new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-gm-verify");
            if(index<0||index+1>=args.Length)return;
            var go=new GameObject("Vault asset verification");DontDestroyOnLoad(go);
            var qa=go.AddComponent<VaultAssetVerification>();qa.folder=args[index+1];qa.persistence=args.Contains("-gm-persistence");
            Directory.CreateDirectory(qa.folder);Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            qa.started=Time.realtimeSinceStartup;Application.logMessageReceived+=qa.OnLog;
            SceneManager.sceneLoaded+=qa.Loaded;qa.Loaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);
            qa.StartCoroutine(qa.Guarded(qa.Run()));
        }
        void Loaded(Scene scene,LoadSceneMode mode)
        {
            effects=FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include);
            foreach(var listener in FindObjectsByType<AudioListener>())
                if(listener.GetComponent<VerificationAudioProbe>()==null)listener.gameObject.AddComponent<VerificationAudioProbe>();
        }
        void Update()
        {
            foreach(var effect in effects)
                if(effect!=null&&effect.gameObject.activeInHierarchy&&effect.particleCount>0)emittedEffects.Add(effect.name);
        }
        void OnLog(string message,string stack,LogType type)
        {
            if(type is LogType.Error or LogType.Exception or LogType.Assert)errors.Add(message+"\n"+stack);
        }
        IEnumerator Guarded(IEnumerator iterator)
        {
            var stack=new Stack<IEnumerator>();stack.Push(iterator);
            while(stack.Count>0)
            {
                object current=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
                catch(Exception e){failures.Add(e.ToString());Finish();yield break;}
                if(!moved){stack.Pop();continue;}
                if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
            Finish();
        }
        void Check(bool condition,string message)
        {
            if(!condition)throw new Exception("Verification failed: "+message);
            passes.Add(message);
            if(!message.StartsWith("Materials supported")&&!message.StartsWith("Text fits"))Debug.Log("QA_PASS "+message);
        }
        void Record(bool condition,string message)
        {
            if(condition)passes.Add(message);else failures.Add(message);
        }
        static T Find<T>() where T:Component=>FindObjectsByType<T>(FindObjectsInactive.Include).FirstOrDefault(x=>x.gameObject.scene==SceneManager.GetActiveScene());
        static ChoicePanel Panel=>Find<ChoicePanel>();
        IEnumerator Until(Func<bool> predicate,string label,float timeout=25)
        {
            float deadline=Time.realtimeSinceStartup+timeout;
            while(!predicate()&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(predicate(),label);
        }
        IEnumerator Stable()=>Until(()=>!ScreenFader.IsBusy,"Scene fade completed");
        IEnumerator Choice(string name)
        {
            yield return Until(()=>SceneManager.GetActiveScene().name==name&&Panel!=null&&Panel.gameObject.activeInHierarchy&&!ScreenFader.IsBusy,"Ready for "+name);
            Canvas.ForceUpdateCanvases();
            var buttons=Panel.buttonContainer.GetComponentsInChildren<ChoiceButton>();
            Check(buttons.Length==Find<SceneDirector>().choices.Count,name+" has the expected number of choices");
            Check(buttons.All(b=>b.icon.sprite!=null&&b.icon.preserveAspect),name+" choice icons are present and retain their proportions");
            Check(buttons.All(b=>b.GetComponent<Button>().IsInteractable()),name+" choices are interactive");
        }
        IEnumerator PressKey(Key key)
        {
            var keyboard=Keyboard.current??InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
        }
        IEnumerator Capture(string label)
        {
            Canvas.ForceUpdateCanvases();
            var path=Path.Combine(folder,(++shot).ToString("D3")+"-"+label+".png");
            // Render the live game camera and UI explicitly; hidden windows have no backbuffer screenshot.
            var camera=Camera.main;
            var canvases=FindObjectsByType<Canvas>().Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var oldCameras=canvases.Select(c=>c.worldCamera).ToArray();
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceCamera;canvases[i].worldCamera=camera;canvases[i].planeDistance=.5f;}
            int width=Mathf.Max(640,Screen.width),height=Mathf.Max(480,Screen.height);
            var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);rt.Create();
            float oldAspect=camera.aspect;camera.targetTexture=rt;camera.aspect=(float)width/height;Canvas.ForceUpdateCanvases();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;camera.aspect=oldAspect;Destroy(texture);rt.Release();Destroy(rt);
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceOverlay;canvases[i].worldCamera=oldCameras[i];}
            Canvas.ForceUpdateCanvases();
            Check(File.Exists(path)&&new FileInfo(path).Length>1000,"Captured "+label);
            yield return null;
        }
        void InspectScene()
        {
            var objects=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true));
            Check(objects.All(t=>t.GetComponents<Component>().All(c=>c!=null)),SceneManager.GetActiveScene().name+" has no missing scripts");
            foreach(var renderer in objects.SelectMany(t=>t.GetComponents<Renderer>()))
                Check(renderer.sharedMaterials.All(m=>m!=null&&m.shader!=null&&m.shader.isSupported),"Materials supported: "+renderer.name);
            foreach(var text in FindObjectsByType<Text>())
            {
                var rect=text.rectTransform.rect;
                if(rect.width<=0||rect.height<=0||string.IsNullOrEmpty(text.text))continue;
                if(text.verticalOverflow==VerticalWrapMode.Truncate)
                    Record(text.preferredHeight<=rect.height+4,"Text fits: "+text.name+" / "+text.text.Replace('\n',' '));
            }
            foreach(var button in FindObjectsByType<Button>())
            {
                var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                Record(corners.All(p=>p.x>=-1&&p.y>=-1&&p.x<=Screen.width+1&&p.y<=Screen.height+1),"Button stays inside viewport: "+button.name);
            }
        }
        void ClickChoice(string label)
        {
            var button=Panel.buttonContainer.GetComponentsInChildren<ChoiceButton>().Single(b=>b.label.text==label);
            button.GetComponent<Button>().onClick.Invoke();
        }
        IEnumerator Failure(string label,string target,string origin,bool timeout=false,bool alreadyChosen=false)
        {
            float began=Time.time;
            if(!timeout&&!alreadyChosen)ClickChoice(label);
            yield return Until(()=>SceneManager.GetActiveScene().name==target&&!ScreenFader.IsBusy,"Entered "+target,timeout?16:10);
            if(timeout)Check(Time.time-began>=7.7f&&Time.time-began<=10.5f,"Laser countdown expires at eight seconds plus the scene fade");
            yield return new WaitForSeconds(.45f);yield return Capture(target+"-early");
            yield return new WaitForSeconds(.75f);yield return Capture(target+"-action");
            yield return Until(()=>Find<SceneDirector>()?.failPanel.activeInHierarchy==true,target+" finished its gag");
            Check(GameFlow.LastChoiceScene==origin,target+" remembers the proper retry scene");
            Check(OutcomeJournal.Found(target),target+" records discovery");
            InspectScene();yield return Capture(target+"-result");
            Find<SceneDirector>().failPanel.GetComponentInChildren<Button>().onClick.Invoke();
            yield return Choice(origin);
            Check(!FindObjectsByType<ParticleSystem>().Any(p=>p.gameObject.scene.name==target),target+" effects do not survive retry");
            yield return Capture(target+"-retry");
        }
        IEnumerator Run()
        {
            yield return new WaitForSeconds(.6f);yield return Stable();
            Check(SceneManager.GetActiveScene().name=="MainMenu","Build starts at MainMenu");
            if(persistence)
            {
                Check(OutcomeJournal.Vault.All(e=>OutcomeJournal.Found(e.Id)),"All 14 outcomes persist across a player restart");
                Check(GameAudio.Volume==7&&GameAudio.Muted,"Sound volume and mute persist across restart");
                var gallery=Find<OutcomeGallery>();gallery.Open();
                Check(gallery.progress.text=="14 / 14 discovered","Gallery displays all discoveries after restart");
                InspectScene();yield return Capture("persisted-gallery");gallery.Close();
                var sound=Find<SoundOptions>();sound.Show();
                Check(sound.percentage.text.Contains("35%")&&sound.percentage.text.Contains("muted"),"Sound panel displays persisted settings");
                yield return Capture("persisted-sound");yield break;
            }
            foreach(var entry in OutcomeJournal.Vault)PlayerPrefs.SetInt(OutcomeJournal.PreferenceKey(entry.Id),0);
            GameAudio.SetVolume(20);if(GameAudio.Muted)GameAudio.ToggleMute();PlayerPrefs.Save();
            InspectScene();yield return Capture("title-1280x720");
            var play=GameObject.Find("PlayButton").GetComponent<Button>();
            EventSystem.current.SetSelectedGameObject(play.gameObject);
            yield return PressKey(Key.DownArrow);
            Check(EventSystem.current.currentSelectedGameObject?.name=="Outcomes","Keyboard navigation reaches Outcomes");
            yield return PressKey(Key.RightArrow);
            Check(EventSystem.current.currentSelectedGameObject?.name=="Sound","Keyboard navigation reaches Sound");
            yield return PressKey(Key.RightArrow);
            Check(EventSystem.current.currentSelectedGameObject?.name=="Credits","Keyboard navigation reaches Credits");
            yield return PressKey(Key.Enter);yield return null;
            var credits=Find<AssetCredits>();Check(credits.gameObject.activeInHierarchy,"Keyboard submit opens Credits");
            InspectScene();yield return Capture("credits");
            yield return PressKey(Key.Escape);Check(!credits.gameObject.activeInHierarchy,"Escape closes Credits");
            Check(EventSystem.current.currentSelectedGameObject==credits.open.gameObject,"Credits returns focus to its opener");
            var soundOptions=Find<SoundOptions>();soundOptions.Show();
            soundOptions.Adjust(7);Check(Mathf.Approximately(AudioListener.volume,.35f),"Volume slider applies 35 percent");
            InspectScene();yield return Capture("sound-controls");
            soundOptions.Mute();Check(GameAudio.Muted&&AudioListener.volume==0,"Mute silences the shared listener");
            soundOptions.Mute();Check(!GameAudio.Muted&&Mathf.Approximately(AudioListener.volume,.35f),"Unmute restores the chosen volume");
            soundOptions.Adjust(20);soundOptions.Back();
            Check(EventSystem.current.currentSelectedGameObject==soundOptions.open.gameObject,"Sound panel restores menu focus");
            var galleryMenu=Find<OutcomeGallery>();galleryMenu.Open();Check(galleryMenu.progress.text=="0 / 14 discovered","Gallery starts empty for the isolated test");galleryMenu.Close();
            Screen.SetResolution(1024,768,false);yield return new WaitForSeconds(.5f);InspectScene();yield return Capture("title-1024x768");
            Find<AssetCredits>().Show();yield return null;InspectScene();yield return Capture("credits-1024x768");Find<AssetCredits>().Close();
            Screen.SetResolution(1280,720,false);yield return new WaitForSeconds(.5f);
            GameObject.Find("PlayButton").GetComponent<Button>().onClick.Invoke();yield return Choice("Choice_1");
            InspectScene();yield return Capture("Choice_1");
            var pad=InputSystem.AddDevice<XInputController>();
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadRight));yield return null;yield return null;
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
            Check(EventSystem.current.currentSelectedGameObject?.GetComponent<ChoiceButton>()?.label.text=="Pizza Guy","Simulated gamepad navigation changes choice focus");
            Check(Find<ControlHints>().confirm.sprite.name=="xbox_button_a","Xbox prompts follow simulated Xbox input");
            InputSystem.RemoveDevice(pad);yield return PressKey(Key.LeftArrow);
            Check(EventSystem.current.currentSelectedGameObject?.GetComponent<ChoiceButton>()?.label.text=="Pogo Stick","Keyboard navigation returns to the first choice");
            var first=Panel.buttonContainer.GetComponentsInChildren<ChoiceButton>().First(b=>b.label.text=="Pogo Stick");
            var rect=(RectTransform)first.transform;var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var mouse=Mouse.current??InputSystem.AddDevice<Mouse>();
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
            yield return Failure("Pogo Stick","Fail_Pogo","Choice_1",false,true);
            yield return Failure("Pizza Guy","Fail_Pizza","Choice_1");
            yield return Failure("Cannonball","Fail_Cannon","Choice_1");
            ClickChoice("Portal Jar");yield return new WaitForSeconds(1.5f);yield return Capture("portal-departure");yield return Choice("Choice_2");
            InspectScene();yield return Capture("Choice_2");
            yield return Failure("Statue","Fail_Statue","Choice_2");
            yield return Failure("Rolling Chair","Fail_Chair","Choice_2");
            yield return Failure("Fire Alarm","Fail_Alarm","Choice_2");
            ClickChoice("Lullabies");yield return new WaitForSeconds(2);yield return Capture("lullabies-guards");yield return Choice("Choice_3");
            InspectScene();yield return Capture("Choice_3");
            yield return Failure("Guess the PIN","Fail_PIN","Choice_3");
            yield return Failure("Knock Politely","Fail_Knock","Choice_3");
            ClickChoice("Library Card");yield return new WaitForSeconds(1.3f);yield return Capture("library-card");yield return Choice("Choice_4");
            InspectScene();yield return Capture("Choice_4");
            yield return Failure("Mail Cart","Fail_Cart","Choice_4");
            yield return Failure("Unplug It","Fail_Unplug","Choice_4");
            yield return Failure("Selfie First","Fail_Selfie","Choice_4");
            float timerStart=Time.time;yield return Failure("timeout","Fail_Frozen","Choice_4",true);
            Check(Time.time-timerStart>=8,"Laser timeout does not fire before its eight-second countdown");
            Screen.SetResolution(1024,768,false);yield return new WaitForSeconds(.4f);InspectScene();yield return Capture("Choice_4-1024x768");
            ClickChoice("Floor Slide");yield return new WaitForSeconds(1.2f);yield return Capture("floor-slide");yield return Choice("Choice_5");
            Screen.SetResolution(1280,720,false);yield return new WaitForSeconds(.4f);InspectScene();yield return Capture("Choice_5");
            yield return Failure("Rocket Skates","Fail_Skates","Choice_5");
            ClickChoice("Night Bus");yield return new WaitForSeconds(2.5f);yield return Capture("bus-boarding");
            yield return Until(()=>SceneManager.GetActiveScene().name=="Victory"&&Find<SceneDirector>()?.winPanel.activeInHierarchy==true&&!ScreenFader.IsBusy,"Night bus reaches victory",30);
            Check(OutcomeJournal.Found("Victory"),"Victory is recorded");InspectScene();yield return Capture("victory");
            Find<SceneDirector>().winPanel.GetComponentInChildren<Button>().onClick.Invoke();yield return Until(()=>SceneManager.GetActiveScene().name=="MainMenu"&&!ScreenFader.IsBusy,"Victory returns to main menu");
            Check(OutcomeJournal.Vault.All(e=>OutcomeJournal.Found(e.Id)),"All 13 failures and victory were discovered through gameplay");
            galleryMenu=Find<OutcomeGallery>();galleryMenu.Open();Check(galleryMenu.progress.text=="14 / 14 discovered","Gallery displays all fourteen outcomes");yield return Capture("complete-gallery");galleryMenu.Close();
            soundOptions=Find<SoundOptions>();soundOptions.Show();soundOptions.Adjust(7);soundOptions.Mute();soundOptions.Back();PlayerPrefs.Save();
            foreach(var name in new[]{"Portal motes","Cannon smoke","Fountain spray","Crash smoke"})
                Record(emittedEffects.Contains(name),"Effect emitted visible particles during gameplay: "+name);
            Record(VerificationAudioProbe.Samples>0&&VerificationAudioProbe.Peak>0,"Actual mixed audio was measured during the playthrough");
            Record(VerificationAudioProbe.Peak<=1.001f,"Mixed audio stays below digital clipping");
            yield return Capture("final-title");
        }
        void Finish()
        {
            var lines=new List<string>{"Passed checks: "+passes.Count,"Failures: "+failures.Count,"Runtime errors: "+errors.Count,"Elapsed seconds: "+(Time.realtimeSinceStartup-started),"Audio samples measured: "+VerificationAudioProbe.Samples,"Maximum mixed peak before silent test monitoring: "+VerificationAudioProbe.Peak};
            lines.AddRange(failures.Select(x=>"FAIL "+x));lines.AddRange(errors.Select(x=>"ERROR "+x));lines.AddRange(passes.Select(x=>"PASS "+x));
            File.WriteAllLines(Path.Combine(folder,"verification.txt"),lines);
            Application.Quit(failures.Count+errors.Count==0?0:1);
        }
    }

    // Captures the game's actual mix while keeping overnight checks inaudible.
    public sealed class VerificationAudioProbe : MonoBehaviour
    {
        public static long Samples;
        public static float Peak;
        void OnAudioFilterRead(float[] data,int channels)
        {
            for(int i=0;i<data.Length;i++){float value=Mathf.Abs(data[i]);if(value>Peak)Peak=value;data[i]=0;}
            Samples+=data.Length;
        }
    }
}
#endif

