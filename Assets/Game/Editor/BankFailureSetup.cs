using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GordonMann.EditorTools
{
    public static class BankFailureSetup
    {
        const string Art = "Assets/Game/Blockout/Bank Failures";
        const string Scenes = "Assets/Game/Blockout/Scenes";
        const string Rig = "Assets/Game/Blockout/Lullabies/Gordon Mann v005 Lullabies.fbx";

        public static void Install()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Rig);
            if (model == null) throw new InvalidOperationException("Missing the Gordon Mann v005 export.");

            InstallOne(model, BankFailureSequence.Gag.Pogo, "Pogo Stick", "Pogo Spring", "Pogo Launch", "Offscreen Crash");
            InstallOne(model, BankFailureSequence.Gag.Pizza, "Pizza Box", "Intercom Knock", "Intercom Refusal", "Box Setdown");
            InstallOne(model, BankFailureSequence.Gag.Cannon, "Cartoon Cannon", "Cannon Fuse", "Cartoon Pop", "Offscreen Crash");
            AssetDatabase.SaveAssets();
        }

        static void InstallOne(GameObject model, BankFailureSequence.Gag gag, string propName,
            string first, string second, string third)
        {
            var firstCue = LoadCue(first);
            var secondCue = LoadCue(second);
            var punchlineCue = LoadCue(third);
            var scene = EditorSceneManager.OpenScene($"{Scenes}/Fail_{gag}.unity");
            var director = Root(scene, "SceneDirector").GetComponent<SceneDirector>();
            if (director.bankFailure != null) return;

            var props = Root(scene, "Vault Job Blockout").transform.Find("Props");
            var stand = Root(scene, "Gordon");
            var sequence = new GameObject($"{gag} Sequence").AddComponent<BankFailureSequence>();
            director.bankFailure = sequence;
            sequence.gag = gag;
            sequence.prop = props.Find(propName);
            sequence.gordon = PortalJarSetup.CreateActor(model, stand);
            sequence.gordon.SetParent(sequence.transform, true);
            stand.SetActive(false);
            sequence.speaker = sequence.gameObject.AddComponent<AudioSource>();
            sequence.speaker.playOnAwake = false;
            sequence.firstCue = firstCue;
            sequence.secondCue = secondCue;
            sequence.punchlineCue = punchlineCue;

            switch (gag)
            {
                case BankFailureSequence.Gag.Pogo:
                    sequence.actionMark = Mark(sequence.transform, "Footrests", Vector3.zero);
                    sequence.closeShot = Shot(sequence, "Pogo close shot", new Vector3(3.7f, 3.8f, -9.8f), new Vector3(-1.62f, 1.8f, .35f));
                    sequence.wideShot = Shot(sequence, "Roof shot", new Vector3(5, 6.1f, -13.8f), new Vector3(.8f, 4.7f, 1.3f));
                    StagePogo(sequence);
                    break;
                case BankFailureSequence.Gag.Pizza:
                    sequence.gordon.SetPositionAndRotation(new Vector3(-2.52f, 0, 2.68f), Quaternion.Euler(0, 75, 0));
                    sequence.prop.SetPositionAndRotation(new Vector3(-1.85f, .89f, 3.18f), Quaternion.Euler(0, -8, 0));
                    props.Find("Pizza Delivery Stand").position = new Vector3(-1.85f, .4f, 3.18f);
                    var intercom = props.Find("Intercom");
                    intercom.position = new Vector3(-1.53f, 1.43f, 3.77f);
                    var red = Material("Tomato Red", new Color32(200, 65, 42, 255));
                    var lit = Material("Refusal Lamp", new Color32(255, 81, 54, 255), true);
                    var cream = Material("Pizza Label", new Color32(252, 225, 155, 255));
                    Piece(sequence.prop, "Printed lid", PrimitiveType.Cube, new Vector3(0, .515f, 0), new Vector3(.78f, .06f, .68f), red);
                    Piece(sequence.prop, "Pizza emblem", PrimitiveType.Cylinder, new Vector3(0, .555f, 0), new Vector3(.41f, .015f, .41f), cream);
                    sequence.effect = Piece(intercom, "Refusal lamp", PrimitiveType.Sphere, new Vector3(0, .27f, -.59f),
                        new Vector3(.64f, .27f, .22f), lit);
                    sequence.effect.SetActive(false);
                    sequence.closeShot = Shot(sequence, "Intercom close shot", new Vector3(-4.9f, 2.6f, -.7f), new Vector3(-1.84f, 1.4f, 3.32f));
                    StagePizzaShot(sequence);
                    break;
                case BankFailureSequence.Gag.Cannon:
                    sequence.gordon.SetPositionAndRotation(new Vector3(-2.36f, 0, -.98f), Quaternion.Euler(0, 73, 0));
                    sequence.actionMark = Mark(sequence.transform, "Cannon launch point", new Vector3(-.77f, .66f, -.68f));
                    sequence.closeShot = Shot(sequence, "Fuse close shot", new Vector3(3.45f, 3.54f, -8), new Vector3(-1.1f, 1.64f, 0));
                    sequence.wideShot = Shot(sequence, "Launch shot", new Vector3(4.8f, 6.5f, -14.8f), new Vector3(3, 3.55f, 1));
                    var cloud = Mark(sequence.transform, "Cartoon smoke", new Vector3(-.63f, 1.45f, -.54f));
                    var vapor = Material("Smoke", new Color32(176, 180, 185, 255));
                    Piece(cloud, "Center Puff", PrimitiveType.Sphere, new Vector3(.06f, .09f, 0), new Vector3(1.08f, .8f, .76f), vapor);
                    Piece(cloud, "Upper Puff", PrimitiveType.Sphere, new Vector3(.25f, .49f, -.06f), Vector3.one * .61f, vapor);
                    Piece(cloud, "Trailing Puff", PrimitiveType.Sphere, new Vector3(-.36f, -.08f, -.25f), Vector3.one * .53f, vapor);
                    sequence.effect = cloud.gameObject;
                    sequence.effect.SetActive(false);
                    break;
            }

            Save(scene);
            Debug.Log($"Installed {gag} failure gag with Gordon Mann v005 in {scene.path}.");
        }

        static void Save(Scene scene)
        {
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + scene.path);
        }

        static void StagePogo(BankFailureSequence sequence)
        {
            var facing = Quaternion.Euler(0, 128, 0);
            sequence.prop.rotation = facing;
            var shaft = sequence.prop.Find("Shaft");
            shaft.localPosition = new Vector3(0, .62f, 0);
            shaft.localScale = new Vector3(.15f, .61f, .15f);
            var handle = sequence.prop.Find("Handle");
            handle.localPosition = new Vector3(0, 1.20f, 0);
            handle.localScale = new Vector3(.92f, .10f, .12f);
            sequence.actionMark.position = sequence.prop.position - facing * Vector3.forward * .19f + Vector3.up * .325f;
            sequence.gordon.SetPositionAndRotation(new Vector3(-2.39f, 0, .64f), facing);
            sequence.closeShot.LookAt(new Vector3(-1.83f, 1.58f, .2f));
        }

        static void StagePizzaShot(BankFailureSequence sequence)
        {
            sequence.closeShot.position = new Vector3(.95f, 2.90f, -1.95f);
            sequence.closeShot.LookAt(new Vector3(-1.96f, 1.27f, 3.31f));
        }

        static GameObject Root(Scene scene, string name) => scene.GetRootGameObjects().Single(o => o.name == name);

        static AudioClip LoadCue(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Art}/{name}.wav");
            return clip != null ? clip : throw new FileNotFoundException("Missing cue " + name);
        }

        static Transform Mark(Transform parent, string name, Vector3 position)
        {
            var mark = new GameObject(name).transform;
            mark.SetParent(parent, false);
            mark.position = position;
            return mark;
        }

        static Transform Shot(BankFailureSequence sequence, string name, Vector3 eye, Vector3 target)
        {
            var mark = Mark(sequence.transform, name, eye);
            mark.LookAt(target);
            return mark;
        }

        static Material Material(string name, Color color, bool unlit = false)
        {
            string path = $"{Art}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"))
            {
                color = color
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static GameObject Piece(Transform parent, string name, PrimitiveType shape, Vector3 position, Vector3 size, Material material)
        {
            var piece = GameObject.CreatePrimitive(shape);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = size;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(piece.GetComponent<Collider>());
            return piece;
        }
    }
}
