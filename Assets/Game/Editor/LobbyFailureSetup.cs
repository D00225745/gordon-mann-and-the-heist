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
    public static class LobbyFailureSetup
    {
        const string Art = "Assets/Game/Blockout/Lobby Failures";
        const string Scenes = "Assets/Game/Blockout/Scenes";
        static readonly Color Steel = Tint(0x626F7C), Ink = Tint(0x23303B), Teal = Tint(0x38747B);
        static readonly Color Amber = Tint(0xDBA743), Red = Tint(0xC83C47), Stone = Tint(0x919D9F);

        public static void Install()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Art}/Gordon Mann v005 Lobby Failures.fbx");
            if (model == null || !model.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r => r.sharedMesh.blendShapeCount == 6))
                throw new InvalidOperationException("Missing lobby failure pose targets from Gordon v005.");
            var chair = RequireProp("Office Chair", BuildChair);
            var firefighter = RequireProp("Fire Crew", BuildFirefighter);
            var statue = StartScene(LobbyFailureSequence.Gag.Statue, model, "Nose Tickle", "Sneeze", "Guard Stare");
            if (statue != null) Statue(statue);
            var rolling = StartScene(LobbyFailureSequence.Gag.Chair, model, "Caster Rattle", "Fountain Splash", "Wheel Skid");
            if (rolling != null) Chair(rolling, chair);
            var alarm = StartScene(LobbyFailureSequence.Gag.Alarm, model, "Alarm Click", "Lobby Alarm", "Crew Arrival");
            if (alarm != null) Alarm(alarm, firefighter);
            AssetDatabase.SaveAssets();
        }

        static LobbyFailureSequence StartScene(LobbyFailureSequence.Gag gag, GameObject model, string first, string impact, string last)
        {
            var scene = EditorSceneManager.OpenScene($"{Scenes}/Fail_{gag}.unity");
            var director = Root(scene, "SceneDirector").GetComponent<SceneDirector>();
            if (director.lobbyFailure != null) return null;
            var sequence = new GameObject($"{gag} Sequence").AddComponent<LobbyFailureSequence>();
            director.lobbyFailure = sequence;
            sequence.gag = gag;
            var stand = Root(scene, "Gordon");
            sequence.gordon = PortalJarSetup.CreateActor(model, stand);
            sequence.gordon.SetParent(sequence.transform, true);
            stand.SetActive(false);
            sequence.speaker = sequence.gameObject.AddComponent<AudioSource>();
            sequence.speaker.playOnAwake = false;
            sequence.firstCue = Audio(first);
            sequence.impactCue = Audio(impact);
            sequence.lastCue = Audio(last);
            return sequence;
        }

        static void Statue(LobbyFailureSequence sequence)
        {
            var scene = sequence.gameObject.scene;
            var set = Root(scene, "Vault Job Blockout").transform;
            var pedestal = set.Find("Architecture/Statue Plinth");
            sequence.gordon.SetPositionAndRotation(V(3.9f, .36f, .8f), Quaternion.Euler(0, 158, 0));
            var stone = Material("Statue Stone", Stone);
            foreach (var renderer in sequence.gordon.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => stone).ToArray();
            var plate = Mark(pedestal.parent, "Plaque", V(3.9f, .207f, .121f));
            Block(plate, "Mount", Vector3.zero, V(.46f, .16f, .047f), Material("Plaque Brass", Amber));
            for (int i = 0; i < 2; i++)
                Block(plate, "Engraving", V(0, .029f - .056f * i, -.025f), V(.27f - .072f * i, .015f, .008f), Material("Engraving", Ink));
            var guard = set.Find("Props/Patrol Guard");
            guard.position = V(2.02f, 0, 3.93f);
            guard.rotation = Quaternion.Euler(0, 23, 0);
            sequence.patrolHead = LullabiesSetup.RigGuard(guard).head;
            sequence.closeShot = Shot(sequence.transform, "Statue close shot", V(6.02f, 3.25f, -4.66f), V(3.87f, 1.43f, .84f));
            sequence.reactionShot = Shot(sequence.transform, "Patrol reaction shot", V(6.4f, 4.11f, -5.75f), V(3.06f, 1.34f, 2.12f));
            sequence.effect = Mark(sequence.transform, "Stone dust", V(4.13f, 2.01f, .31f));
            var dust = Material("Stone Dust", Tint(0xC7CBC2));
            Shape(sequence.effect, "Puff", PrimitiveType.Sphere, V(0, 0, 0), V(.26f, .17f, .18f), dust);
            Shape(sequence.effect, "Puff", PrimitiveType.Sphere, V(.18f, -.03f, -.18f), V(.21f, .17f, .22f), dust);
            Shape(sequence.effect, "Puff", PrimitiveType.Sphere, V(.28f, .11f, -.21f), V(.13f, .12f, .18f), dust);
            sequence.effect.gameObject.SetActive(false);
            StageDust(sequence);
            Save(scene);
        }

        static void Chair(LobbyFailureSequence sequence, GameObject chair)
        {
            var scene = sequence.gameObject.scene;
            var set = Root(scene, "Vault Job Blockout").transform;
            var props = set.Find("Props");
            props.Find("Rolling Chair").gameObject.SetActive(false);
            sequence.prop = Place(chair, props, V(-1.72f, 0, -.87f));
            sequence.chairTurn = Mark(sequence.transform, "Curve tangent", V(.77f, 0, -.60f));
            sequence.actionMark = Mark(sequence.transform, "Front of fountain", V(2.08f, 0, 1.00f));
            sequence.prop.rotation = Quaternion.LookRotation(sequence.chairTurn.position - sequence.prop.position);
            sequence.casters = sequence.prop.Find("Caster rig").Cast<Transform>().Select(wheel => wheel.Find("Tire pivot")).ToArray();
            sequence.gordon.SetParent(sequence.prop, false);
            sequence.gordon.SetLocalPositionAndRotation(V(0, .078f, .035f), Quaternion.identity);
            sequence.closeShot = Shot(sequence.transform, "Follow the chair", V(5.5f, 4.7f, -8.2f), V(.23f, 1.18f, .15f));
            sequence.reactionShot = Shot(sequence.transform, "Fountain impact shot", V(6.15f, 3.72f, -3.91f), V(2.23f, 1.12f, 1.73f));
            sequence.effect = Mark(sequence.transform, "Fountain droplets", V(2.59f, .57f, 1.73f));
            var paleBlue = Material("Fountain Spray", Tint(0xA7D8E6));
            for (int i = 0; i < 11; i++)
            {
                float rad = i * 2.399963f;
                float span = .34f + .079f * i;
                Shape(sequence.effect, "Droplet", PrimitiveType.Sphere, V(Mathf.Cos(rad) * span, 0, Mathf.Sin(rad) * span),
                    Vector3.one * .20f, paleBlue);
            }
            sequence.effect.gameObject.SetActive(false);
            StageSplash(sequence);
            Save(scene);
        }

        static void Alarm(LobbyFailureSequence sequence, GameObject crew)
        {
            var scene = sequence.gameObject.scene;
            var set = Root(scene, "Vault Job Blockout").transform;
            var props = set.Find("Props");
            var alarm = props.Find("Fire Alarm");
            var lever = Mark(props, "Alarm lever hinge", V(-2.8f, 1.611f, 6.092f));
            var plate = Material("Alarm Face", Tint(0x992E42));
            Block(lever, "Pull plate", V(0, -.081f, -.015f), V(.258f, .174f, .040f), plate);
            Block(lever, "Grip stripe", V(0, -.080f, -.044f), V(.195f, .041f, .029f), Material("Bright Stripe", Tint(0xFFDE9C)));
            sequence.prop = lever;
            sequence.actionMark = Mark(sequence.transform, "Caught by the crew", Vector3.zero);
            sequence.closeShot = Shot(sequence.transform, "Alarm plate close shot", V(.14f, 2.87f, 1.93f), V(-2.99f, 1.34f, 5.93f));
            sequence.reactionShot = Shot(sequence.transform, "Crew arrives wide shot", V(3.08f, 4.39f, -2.18f), V(-2.02f, 1.19f, 4.47f));
            sequence.effect = props.Find("Alarm Light");
            sequence.effect.GetComponent<Renderer>().sharedMaterial = Material("Alarm Lens", Tint(0xFF5B67), true);
            sequence.effect.gameObject.SetActive(false);
            sequence.beacon = Mark(props, "Alarm glow", V(-2.8f, 2.62f, 5.71f)).gameObject.AddComponent<Light>();
            sequence.beacon.type = LightType.Point;
            sequence.beacon.range = 3.25f;
            sequence.beacon.color = Tint(0xFF314D);
            sequence.beacon.enabled = false;
            sequence.responders = new[]
            {
                Firefighter(sequence, crew, V(.85f, 0, 5.94f), V(-1.26f, 0, 5.43f)),
                Firefighter(sequence, crew, V(1.45f, 0, 5.56f), V(-.70f, 0, 4.60f)),
                Firefighter(sequence, crew, V(2.08f, 0, 5.79f), V(.11f, 0, 5.41f))
            };
            StageAlarm(sequence, alarm);
            Save(scene);
        }

        static void StageDust(LobbyFailureSequence sequence)
        {
            sequence.effect.position = V(4.15f, 2.095f, .36f);
            foreach (var puff in sequence.effect.GetComponentsInChildren<MeshRenderer>(true))
                puff.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void StageSplash(LobbyFailureSequence sequence)
        {
            foreach (var drop in sequence.effect.GetComponentsInChildren<MeshRenderer>(true))
                drop.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void StageAlarm(LobbyFailureSequence sequence, Transform unit)
        {
            unit.position = V(-2.80f, 1.374f, 6.20f);
            sequence.prop.position = V(-2.8f, 1.425f, 6.092f);
            sequence.gordon.SetPositionAndRotation(V(-3.15f, 0, 5.552f), Quaternion.identity);
            sequence.actionMark.SetPositionAndRotation(V(-2.80f, 0, 5.35f), Quaternion.Euler(0, 143, 0));
            sequence.closeShot.position = V(1.55f, 2.80f, 2.97f);
            sequence.closeShot.LookAt(V(-3.02f, 1.30f, 5.69f));
            sequence.reactionShot.position = V(4.35f, 4.20f, -1.24f);
            sequence.reactionShot.LookAt(V(-1.12f, 1.3f, 4.72f));
            unit.parent.Find("Patrol Guard").position = V(4.32f, 0, 4.76f);
            for (int i = 0; i < sequence.responders.Length; i++)
            {
                var responder = sequence.responders[i].actor;
                responder.position = V(2.35f + .84f * i, 0, 5.94f - .30f * i);
                responder.gameObject.SetActive(false);
            }
        }

        static LobbyFailureSequence.Responder Firefighter(LobbyFailureSequence sequence, GameObject crew, Vector3 start, Vector3 finish)
        {
            var actor = Place(crew, sequence.transform, start);
            actor.rotation = Quaternion.LookRotation(finish - start);
            return new LobbyFailureSequence.Responder
            {
                actor = actor, leftLeg = actor.Find("Left boot pivot"), rightLeg = actor.Find("Right boot pivot"),
                stop = Mark(sequence.transform, "Crew stopping mark", finish)
            };
        }

        static void BuildChair(Transform chair)
        {
            var seat = Material("Seat Upholstery", Teal);
            var rim = Material("Office Chair Frame", Steel);
            var black = Material("Caster Rubber", Ink);
            Block(chair, "Seat cushion", V(0, .423f, .004f), V(.62f, .091f, .54f), seat);
            Block(chair, "Seat pan", V(0, .358f, .000f), V(.64f, .055f, .55f), rim);
            Block(chair, "Lumbar support", V(0, .629f, -.30f), V(.56f, .50f, .107f), seat).Rotate(-9, 0, 0);
            Block(chair, "Backrest post", V(0, .51f, -.355f), V(.115f, .38f, .09f), rim);
            foreach (float x in new[] { -.385f, .385f })
            {
                Block(chair, "Armrest post", V(x, .47f, -.09f), V(.043f, .24f, .05f), rim);
                Block(chair, "Armrest pad", V(x, .583f, .009f), V(.085f, .044f, .39f), black);
            }
            Shape(chair, "Gas column", PrimitiveType.Cylinder, V(0, .221f, 0), V(.18f, .132f, .18f), rim);
            var wheels = Mark(chair, "Caster rig", Vector3.zero);
            for (int i = 0; i < 5; i++)
            {
                var branch = Mark(wheels, "Caster " + i, Vector3.zero);
                branch.localRotation = Quaternion.Euler(0, i * 72 + 35, 0);
                Block(branch, "Spoke", V(0, .137f, .176f), V(.09f, .06f, .41f), rim);
                var tire = Mark(branch, "Tire pivot", V(0, .072f, .38f));
                Shape(tire, "Wheel", PrimitiveType.Cylinder, Vector3.zero, V(.144f, .042f, .144f), black).Rotate(0, 0, 90);
                Shape(tire, "Hub", PrimitiveType.Cylinder, Vector3.zero, V(.07f, .046f, .07f), rim).Rotate(0, 0, 90);
            }
        }

        static void BuildFirefighter(Transform actor)
        {
            var uniform = Material("Crew Uniform", Tint(0xA06340));
            var boot = Material("Boot Rubber", Ink);
            var stripe = Material("Crew Reflectors", Tint(0xF6D366));
            var face = Material("Crew Face", Tint(0xA57457));
            var shell = Material("Crew Helmets", Amber);
            Block(actor, "Coat", V(0, .992f, 0), V(.63f, .83f, .38f), uniform);
            Block(actor, "Coat reflector", V(0, .82f, 0), V(.645f, .085f, .395f), stripe);
            Block(actor, "Raised collar", V(0, 1.337f, 0), V(.42f, .14f, .43f), stripe);
            Shape(actor, "Head", PrimitiveType.Sphere, V(0, 1.54f, .067f), V(.407f, .413f, .36f), face);
            Shape(actor, "Helmet crown", PrimitiveType.Sphere, V(0, 1.72f, .014f), V(.54f, .365f, .48f), shell);
            Shape(actor, "Helmet brim", PrimitiveType.Cylinder, V(0, 1.67f, .065f),
                V(.64f, .024f, .57f), shell);
            Block(actor, "Helmet badge", V(0, 1.715f, .281f), V(.14f, .10f, .025f), Material("Crew Badge", Red));
            foreach (float side in new[] { -1f, 1f })
            {
                var leg = Mark(actor, side < 0 ? "Left boot pivot" : "Right boot pivot", V(side * .168f, .625f, 0));
                Block(leg, "Trousers", V(0, -.224f, 0), V(.246f, .454f, .28f), uniform);
                Block(leg, "Boot", V(0, -.533f, .056f), V(.263f, .187f, .36f), boot);
                Block(leg, "Leg reflector", V(0, -.383f, 0), V(.257f, .065f, .29f), stripe);
                var arm = Block(actor, "Sleeve", V(side * .367f, 1.046f, .06f), V(.23f, .62f, .237f), uniform);
                arm.Rotate(-17, 0, side * 14);
                Shape(actor, "Glove", PrimitiveType.Sphere, V(side * .419f, .77f, .158f), V(.238f, .216f, .227f), boot);
                Block(actor, "Eye", V(.064f * side, 1.553f, .237f), V(.035f, .047f, .025f), boot);
            }
        }

        static GameObject RequireProp(string name, Action<Transform> build)
        {
            var path = $"{Art}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = new GameObject(name);
            build(root.transform);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved != null ? saved : throw new IOException("Unable to save " + path);
        }

        static Transform Place(GameObject asset, Transform parent, Vector3 position)
        {
            var transform = ((GameObject)PrefabUtility.InstantiatePrefab(asset, parent)).transform;
            transform.localPosition = position;
            return transform;
        }

        static Transform Mark(Transform parent, string title, Vector3 at)
        {
            var mark = new GameObject(title).transform;
            mark.SetParent(parent, false);
            mark.localPosition = at;
            return mark;
        }

        static Transform Shot(Transform group, string title, Vector3 position, Vector3 target)
        {
            var view = Mark(group, title, position);
            view.LookAt(target);
            return view;
        }

        static Transform Block(Transform parent, string name, Vector3 position, Vector3 size, Material material) =>
            Shape(parent, name, PrimitiveType.Cube, position, size, material);

        static Transform Shape(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(primitive);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj.transform;
        }

        static Material Material(string name, Color tint, bool unlit = false)
        {
            string path = $"{Art}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")) { color = tint };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static AudioClip Audio(string title) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{Art}/{title}.wav") ?? throw new FileNotFoundException("Missing cue " + title);

        static void Save(Scene scene)
        {
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + scene.path);
        }

        static GameObject Root(Scene scene, string name) => scene.GetRootGameObjects().Single(obj => obj.name == name);
        static Color Tint(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    }
}
