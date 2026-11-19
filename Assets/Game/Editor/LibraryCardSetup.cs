using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GordonMann.EditorTools
{
    public static class LibraryCardSetup
    {
        const string Folder = "Assets/Game/Blockout/Library Card";
        const string Scenes = "Assets/Game/Blockout/Scenes";
        static readonly Color Dark = new Color32(23,37,56,255);
        static readonly Color Mint = new Color32(93,242,156,255);
        static readonly Color Pink = new Color32(240,61,106,255);
        static readonly Color Ivory = new Color32(242,230,195,255);
        internal static readonly Vector3 ReaderPosition = new Vector3(-2.06f,.78f,3.72f);
        internal static readonly Vector3 ReaderApproach = new Vector3(-2.47f,0,2.97f);

        public static void Install()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Blockout/Lullabies/Gordon Mann v005 Lullabies.fbx");
            if (model == null) throw new FileNotFoundException("Missing Gordon’s v005 pose export.");
            var security = EditorSceneManager.OpenScene(Scenes + "/Choice_3.unity");
            if (security.GetRootGameObjects().All(g=>g.GetComponent<LibraryCardSequence>()==null))
                AddSecuritySequence(security);
            var hall = EditorSceneManager.OpenScene(Scenes + "/Choice_4.unity");
            var roots = hall.GetRootGameObjects();
            if (roots.All(g=>g.name!="Gordon Mann v005"))
            {
                var placeholder = roots.Single(g=>g.name=="Gordon");
                var actor = PortalJarSetup.CreateActor(model,placeholder);
                actor.rotation = Quaternion.Euler(0,20,0);
                placeholder.SetActive(false);
                Save(hall);
            }
            AssetDatabase.SaveAssets();
        }

        static void AddSecuritySequence(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var props = roots.Single(g=>g.name=="Vault Job Blockout").transform.Find("Props");
            var canvas = roots.Single(g=>g.name=="Canvas").transform;
            var controller = roots.Select(g=>g.GetComponent<SceneDirector>()).Single(d=>d!=null);
            var clip = new GameObject("Library Card Sequence").AddComponent<LibraryCardSequence>();
            clip.actor = roots.Single(g=>g.name=="Gordon Mann v005").transform;
            clip.card = props.Find("Library Card");
            clip.door = props.Find("Security Door Pivot");
            ClipArtwork(clip.card);
            var reader = (GameObject)PrefabUtility.InstantiatePrefab(ReaderPrefab(),props);
            reader.transform.localPosition = ReaderPosition;
            clip.lockedLamp = props.Find("Access Light").gameObject;
            var lamp = Shape(PrimitiveType.Sphere,props,"Accepted Access Light",clip.lockedLamp.transform.localPosition,
                new Vector3(.21f,.21f,.21f),Material("Accepted Lamp",Mint,true));
            clip.openLamp = lamp.gameObject;
            clip.openLamp.SetActive(false);
            var shell = Material("Door Pocket",Dark);
            Shape(PrimitiveType.Cube,props,"Door Pocket Fascia",new Vector3(3.34f,1.61f,3.77f),
                new Vector3(3.12f,3.22f,.22f),shell);
            Shape(PrimitiveType.Cube,props,"Hallway threshold",new Vector3(0,-.08f,4.83f),
                new Vector3(3.17f,.16f,1.78f),shell);
            var wash = Material("Hallway Light",Pink,true);
            for (int i=0;i<3;i++)
                Shape(PrimitiveType.Cube,props,"Hallway laser preview",new Vector3(0,.91f+.22f*i,5.42f+.34f*i),
                    new Vector3(3.14f,.038f,.036f),wash);
            Shape(PrimitiveType.Cube,props,"Hallway return wall",new Vector3(0,1.68f,6.37f),
                new Vector3(3.25f,3.36f,.17f),shell);
            clip.readerMark = Mark(clip.transform,"Reader approach",ReaderApproach);
            clip.scanMark = Mark(clip.transform,"Card scan",ReaderPosition+Vector3.back*.17f);
            clip.scanMark.rotation = Quaternion.Euler(0,0,-8);
            clip.doorway = Mark(clip.transform,"Door approach",new Vector3(0,0,3.2f));
            clip.exit = Mark(clip.transform,"Enter laser hall",new Vector3(0,0,5.19f));
            clip.closeShot = CameraMark(clip.transform,"Reader close shot",new Vector3(.95f,3.05f,-1.73f),new Vector3(-2.19f,1.23f,3.18f));
            clip.wideShot = CameraMark(clip.transform,"Door reveal shot",new Vector3(3.28f,3.84f,-7.9f),new Vector3(-.19f,1.55f,3.95f));
            clip.heading = canvas.Find("Heading").gameObject;
            clip.narration = canvas.Find("Narration").gameObject;
            clip.speaker = clip.gameObject.AddComponent<AudioSource>();
            clip.speaker.playOnAwake = false;
            clip.footstep = Sound("Assets/Game/Blockout/Lullabies/Soft Footstep.wav");
            clip.cardTap = Sound(Folder + "/Card tap.wav");
            clip.accepted = Sound(Folder + "/Reader accepted.wav");
            clip.doorMotor = Sound(Folder + "/Door motor.wav");
            controller.choicePanel.libraryCard = clip;
            Save(scene);
        }

        static GameObject ReaderPrefab()
        {
            string path = Folder + "/Library Card Reader.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var reader = new GameObject("Library Card Reader").transform;
            var ink = Material("Reader Shell",Dark);
            Shape(PrimitiveType.Cube,reader,"Reader housing",Vector3.zero,new Vector3(.69f,.40f,.26f),ink);
            Shape(PrimitiveType.Cube,reader,"Bezel",new Vector3(0,0,-.138f),new Vector3(.57f,.31f,.035f),Material("Reader Bezel",new Color32(78,109,127,255)));
            Shape(PrimitiveType.Cube,reader,"Scan surface",new Vector3(0,0,-.16f),new Vector3(.43f,.17f,.016f),ink);
            Shape(PrimitiveType.Cube,reader,"Scan stripe",new Vector3(0,.09f,-.174f),new Vector3(.37f,.016f,.012f),Material("Scan Stripe",Mint,true));
            var prefab = PrefabUtility.SaveAsPrefabAsset(reader.gameObject,path);
            UnityEngine.Object.DestroyImmediate(reader.gameObject);
            return prefab;
        }

        static void ClipArtwork(Transform card)
        {
            var material = Material("Printed Card Ink",Dark);
            Shape(PrimitiveType.Cube,card,"Borrower stripe",new Vector3(0,.20f,-.53f),new Vector3(.89f,.16f,.045f),material);
            Shape(PrimitiveType.Cube,card,"Borrower label",new Vector3(-.11f,-.10f,-.53f),new Vector3(.57f,.20f,.045f),
                Material("Printed Card Label",Ivory));
        }

        static Transform Shape(PrimitiveType type,Transform parent,string name,Vector3 point,Vector3 scale,Material material)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent,false);
            obj.transform.localPosition = point;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj.transform;
        }

        static Transform Mark(Transform parent,string name,Vector3 point)
        {
            var mark = new GameObject(name).transform;
            mark.SetParent(parent,false);
            mark.localPosition = point;
            return mark;
        }

        static Transform CameraMark(Transform parent,string name,Vector3 eye,Vector3 toward)
        {
            var camera = Mark(parent,name,eye);
            camera.LookAt(toward);
            return camera;
        }

        static Material Material(string name,Color tint,bool unlit=false)
        {
            string path = $"{Folder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"))
                { name=name,color=tint };
            AssetDatabase.CreateAsset(material,path);
            return material;
        }

        static AudioClip Sound(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path) ??
            throw new FileNotFoundException("Missing sequence audio",path);

        static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + scene.path);
        }
    }
}
