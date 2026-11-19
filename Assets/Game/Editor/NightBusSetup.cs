using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace GordonMann.EditorTools
{
    public static class NightBusSetup
    {
        const string Folder="Assets/Game/Blockout/Night Bus";
        internal const string PrefabPath=Folder+"/Night Bus.prefab";
        const string Scenes="Assets/Game/Blockout/Scenes";
        static readonly Color Navy=new Color32(24,36,48,255);
        static readonly Color Blue=new Color32(39,156,145,255);
        static readonly Color Iron=new Color32(116,145,152,255);
        static readonly Color Gold=new Color32(247,192,97,255);

        public static void Install()
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Blockout/Lullabies/Gordon Mann v005 Lullabies.fbx");
            if(model==null) throw new FileNotFoundException("The v005 model is missing.");
            var getaway=EditorSceneManager.OpenScene($"{Scenes}/Choice_5.unity");
            if(getaway.GetRootGameObjects().All(go=>go.GetComponent<NightBusSequence>()==null))
                AddGetaway(getaway,model);
            var victory=EditorSceneManager.OpenScene($"{Scenes}/Victory.unity");
            var parents=victory.GetRootGameObjects();
            var props=parents.Single(go=>go.name=="Vault Job Blockout").transform.Find("Props");
            var old=props.Find("Night Bus");
            if(old!=null && old.Find("Left boarding leaf")==null)
            {
                var newBus=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),props);
                newBus.transform.SetPositionAndRotation(old.position,old.rotation);
                old.name="Original Night Bus";
                old.gameObject.SetActive(false);
                parents.Single(go=>go.name=="Gordon").SetActive(false);
                Save(victory);
            }
            AssetDatabase.SaveAssets();
        }

        static void AddGetaway(Scene scene,GameObject model)
        {
            var roots=scene.GetRootGameObjects();
            var props=roots.Single(go=>go.name=="Vault Job Blockout").transform.Find("Props");
            var bus=props.Find("Night Bus");
            PrepareBus(bus);
            var prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(bus.gameObject,PrefabPath,InteractionMode.AutomatedAction);
            if(prefab==null) throw new IOException("Could not save the Night Bus prefab.");
            var stage=new GameObject("Night Bus Sequence").AddComponent<NightBusSequence>();
            var placeholder=roots.Single(go=>go.name=="Gordon");
            stage.actor=PortalJarSetup.CreateActor(model,placeholder);
            placeholder.SetActive(false);
            stage.actor.rotation=Quaternion.Euler(0,-57,0);
            stage.bus=bus;
            stage.busArrival=Mark(stage.transform,"Bus arrival",new Vector3(-13.9f,-.06f,2.95f));
            stage.busStop=Mark(stage.transform,"Curb stop",new Vector3(-2.49f,-.06f,2.95f));
            stage.busDeparture=Mark(stage.transform,"Bus departure",new Vector3(15.8f,-.06f,2.95f));
            bus.position=stage.busArrival.position;
            stage.curb=Mark(stage.transform,"Wait at curb",new Vector3(-1.33f,0,.86f));
            stage.step=Mark(stage.transform,"Boarding step",new Vector3(-.15f,.04f,1.82f));
            stage.inside=Mark(stage.transform,"Inside bus",new Vector3(-.15f,.42f,2.73f));
            stage.fareBox=Mark(stage.transform,"Fare slot",new Vector3(.38f,1.13f,2.23f));
            stage.leftDoor=bus.Find("Left boarding leaf");
            stage.rightDoor=bus.Find("Right boarding leaf");
            stage.wheels=bus.Cast<Transform>().Where(item=>item.name.StartsWith("Bus Wheel",StringComparison.Ordinal)).ToArray();
            var coin=Primitive(stage.transform,"Exact change",Vector3.zero,new Vector3(.16f,.018f,.16f),Material("Fare Gold",Gold),PrimitiveType.Cylinder);
            stage.coin=coin;
            coin.gameObject.SetActive(false);
            stage.streetShot=Shot(stage.transform,"Bus arrival wide shot",new Vector3(4.3f,4.65f,-11.2f),new Vector3(-1.30f,1.47f,1.64f));
            stage.boardingShot=Shot(stage.transform,"Boarding close shot",new Vector3(3.25f,3.54f,-4.9f),new Vector3(-.44f,1.47f,2.23f));
            var canvas=roots.Single(go=>go.name=="Canvas").transform;
            stage.heading=canvas.Find("Heading").gameObject;
            stage.narration=canvas.Find("Narration").gameObject;
            stage.speaker=stage.gameObject.AddComponent<AudioSource>();
            stage.speaker.playOnAwake=false;
            stage.engine=Sound(Folder+"/Bus approach.wav");
            stage.brake=Sound(Folder+"/Bus brake.wav");
            stage.doorSound=Sound(Folder+"/Bus doors.wav");
            stage.fareSound=Sound(Folder+"/Exact change.wav");
            stage.footfall=Sound("Assets/Game/Blockout/Lullabies/Soft Footstep.wav");
            roots.Select(go=>go.GetComponent<SceneDirector>()).Single(d=>d!=null).choicePanel.nightBus=stage;
            Save(scene);
        }

        static void PrepareBus(Transform bus)
        {
            var body=bus.Find("Body");
            var busPaint=Material("Bus Paint",Blue);
            var trim=Material("Bus Trim",Navy);
            var metal=Material("Stair Treads",Iron);
            body.localPosition=new Vector3(-.94f,1.44f,0);
            body.localScale=new Vector3(5.02f,2.12f,1.85f);
            body.GetComponent<Renderer>().sharedMaterial=busPaint;
            var roof=bus.Find("Roof");
            roof.localPosition=new Vector3(0,2.59f,0);
            Primitive(bus,"Front nose",new Vector3(3.27f,1.44f,0),new Vector3(.36f,2.12f,1.85f),busPaint);
            Primitive(bus,"Doorway interior",new Vector3(2.34f,1.43f,.72f),new Vector3(1.58f,2.02f,.15f),trim);
            Primitive(bus,"Boarding tread",new Vector3(2.34f,.35f,-.57f),new Vector3(1.52f,.19f,.74f),metal);
            Primitive(bus,"Step edge",new Vector3(2.34f,.20f,-.98f),new Vector3(1.45f,.10f,.22f),metal);
            Primitive(bus,"Door header",new Vector3(2.34f,2.43f,-.02f),new Vector3(1.57f,.20f,1.83f),busPaint);
            var sign=bus.Find("Route Display");
            sign.localPosition=new Vector3(2.34f,2.43f,-.97f);
            sign.localScale=new Vector3(.74f,.13f,.05f);
            var originalDoor=bus.Find("Boarding Door");
            originalDoor.name="Left boarding leaf";
            originalDoor.localPosition=new Vector3(1.96f,1.34f,-.97f);
            originalDoor.localScale=new Vector3(.735f,1.93f,.095f);
            originalDoor.GetComponent<Renderer>().sharedMaterial=trim;
            Primitive(bus,"Right boarding leaf",new Vector3(2.72f,1.34f,-.97f),new Vector3(.735f,1.93f,.095f),trim);
            var windows=bus.Cast<Transform>().Where(item=>item.name=="Night Bus Window").ToArray();
            for(int i=0;i<windows.Length;i++)
            {
                windows[i].name="Passenger window "+(i+1);
                windows[i].localPosition=new Vector3(-2.73f+1.17f*i,1.98f,-.944f);
                windows[i].localScale=new Vector3(1.04f,.72f,.065f);
            }
            Primitive(bus,"Fare box",new Vector3(2.87f,1.00f,-.73f),new Vector3(.23f,.48f,.26f),metal);
            Primitive(bus,"Fare box lid",new Vector3(2.87f,1.25f,-.73f),new Vector3(.26f,.06f,.30f),trim);
            var lights=Material("Headlamp",Gold,true);
            var tails=Material("Tail lamp",new Color32(225,65,62,255),true);
            foreach(float s in new[]{-.66f,.66f})
            {
                Primitive(bus,"Headlamp",new Vector3(3.46f,.69f,s),new Vector3(.065f,.25f,.27f),lights);
                Primitive(bus,"Tail lamp",new Vector3(-3.47f,.69f,s),new Vector3(.065f,.24f,.23f),tails);
            }
            Primitive(bus,"Front bumper",new Vector3(3.49f,.45f,0),new Vector3(.14f,.15f,1.84f),metal);
            Primitive(bus,"Rear bumper",new Vector3(-3.50f,.45f,0),new Vector3(.14f,.15f,1.84f),metal);
            FinishRunningGear(bus);
        }

        internal static void FinishRunningGear(Transform bus)
        {
            var hub=Material("Wheel Hubs",Iron);
            foreach(var wheel in bus.Cast<Transform>().Where(item=>item.name.StartsWith("Bus Wheel",StringComparison.Ordinal)))
            {
                var position=wheel.localPosition;
                if(position.x>0)
                    wheel.localPosition=new Vector3(.94f,position.y,position.z);
                if(wheel.Find("Hub")!=null) continue;
                foreach(float side in new[]{-1f,1f})
                {
                    Primitive(wheel,side<0?"Hub inner":"Hub",new Vector3(0,side*1.035f,0),
                        new Vector3(.51f,.048f,.51f),hub,PrimitiveType.Cylinder);
                    Primitive(wheel,"Axle cap",new Vector3(0,side*1.10f,0),
                        new Vector3(.17f,.032f,.17f),Material("Bus Trim",Navy),PrimitiveType.Cylinder);
                    Primitive(wheel,"Wheel balance mark",new Vector3(.27f,side*1.02f,0),
                        new Vector3(.084f,.075f,.13f),hub);
                }
            }
        }

        static Transform Primitive(Transform parent,string name,Vector3 point,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube)
        {
            var prop=GameObject.CreatePrimitive(type);
            prop.name=name;
            prop.transform.SetParent(parent,false);
            prop.transform.localPosition=point;
            prop.transform.localScale=size;
            prop.GetComponent<Renderer>().sharedMaterial=material;
            Object.DestroyImmediate(prop.GetComponent<Collider>());
            return prop.transform;
        }

        static Transform Mark(Transform parent,string name,Vector3 position)
        {
            var node=new GameObject(name).transform;
            node.SetParent(parent,false);
            node.localPosition=position;
            return node;
        }

        static Transform Shot(Transform parent,string name,Vector3 eye,Vector3 target)
        {
            var node=Mark(parent,name,eye);
            node.LookAt(target);
            return node;
        }

        static AudioClip Sound(string path)=>AssetDatabase.LoadAssetAtPath<AudioClip>(path) ??
            throw new FileNotFoundException("Missing Night Bus cue.",path);

        static Material Material(string name,Color color,bool luminous=false)
        {
            string path=$"{Folder}/{name}.mat";
            var old=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(old!=null) return old;
            var material=new Material(Shader.Find(luminous?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"))
                {name=name,color=color};
            AssetDatabase.CreateAsset(material,path);
            return material;
        }

        static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save "+scene.path);
        }
    }
}
