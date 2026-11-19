using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GordonMann.EditorTools
{
    public static class FloorSlideSetup
    {
        const string Folder = "Assets/Game/Blockout/Floor Slide";
        internal const string ScenePath = "Assets/Game/Blockout/Scenes/Choice_4.unity";

        public static void Install()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var roots=scene.GetRootGameObjects();
            if (roots.Any(g=>g.GetComponent<FloorSlideSequence>()!=null)) return;
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Gordon Mann v005 Floor Slide.fbx");
            if (model==null || !model.GetComponentsInChildren<SkinnedMeshRenderer>()
                .Any(r=>r.sharedMesh.blendShapeCount>=4))
                throw new InvalidOperationException("The v005 Floor Slide poses are missing.");
            var original=roots.Single(g=>g.name=="Gordon Mann v005");
            var canvas=roots.Single(g=>g.name=="Canvas").transform;
            var director=roots.Select(g=>g.GetComponent<SceneDirector>()).Single(d=>d!=null);
            var sequence=new GameObject("Floor Slide Sequence").AddComponent<FloorSlideSequence>();
            sequence.actor=PortalJarSetup.CreateActor(model,original);
            original.name="Gordon Mann v005 - original hallway staging";
            original.SetActive(false);
            sequence.approach=Mark(sequence.transform,"Plant shoes",new Vector3(0,.028f,-3.03f));
            sequence.slideStart=Mark(sequence.transform,"Feet-first launch",new Vector3(0,.028f,-2.22f));
            sequence.slideEnd=Mark(sequence.transform,"Beyond last beam",new Vector3(0,.028f,6.82f));
            sequence.takeoffCamera=Shot(sequence.transform,"Takeoff shot",new Vector3(6.25f,4.4f,-8.7f),new Vector3(-.76f,.90f,-1.5f));
            sequence.trackingCamera=Shot(sequence.transform,"Leaning tracking shot",new Vector3(7.36f,2.92f,-7.69f),new Vector3(0,.65f,.72f));
            sequence.finishCamera=Shot(sequence.transform,"Braking shot",new Vector3(6.15f,3.15f,1.03f),new Vector3(.10f,1.12f,6.77f));
            sequence.heading=canvas.Find("Heading").gameObject;
            sequence.narration=canvas.Find("Narration").gameObject;
            sequence.speaker=sequence.gameObject.AddComponent<AudioSource>();
            sequence.speaker.playOnAwake=false;
            sequence.plant=Sound("Shoe plant");
            sequence.scrape=Sound("Floor scrape");
            sequence.settle=Sound("Slide stop");
            director.choicePanel.floorSlide=sequence;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Floor Slide.");
        }

        static AudioClip Sound(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{name}.wav")
            ?? throw new FileNotFoundException("Missing floor slide sound: " + name);

        static Transform Mark(Transform parent,string name,Vector3 position)
        {
            var marker=new GameObject(name).transform;
            marker.SetParent(parent,false);
            marker.localPosition=position;
            return marker;
        }

        static Transform Shot(Transform parent,string name,Vector3 eye,Vector3 at)
        {
            var marker=Mark(parent,name,eye);
            marker.LookAt(at);
            return marker;
        }
    }
}
