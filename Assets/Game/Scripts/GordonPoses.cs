using System.Collections.Generic;
using UnityEngine;

namespace GordonMann
{
    internal sealed class GordonPoses
    {
        readonly Dictionary<string,List<(SkinnedMeshRenderer mesh,int index)>> channels = new();

        public GordonPoses(Transform actor)
        {
            foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                for (int i=0;i<skin.sharedMesh.blendShapeCount;i++)
                {
                    string channel = skin.sharedMesh.GetBlendShapeName(i);
                    string label = channel.Substring(channel.LastIndexOf('.')+1);
                    if (!channels.TryGetValue(label,out var bindings))
                        channels.Add(label,bindings = new List<(SkinnedMeshRenderer,int)>());
                    bindings.Add((skin,i));
                }
        }

        public void Set(string name,float weight)
        {
            if (!channels.TryGetValue(name,out var bindings)) return;
            foreach (var (mesh,index) in bindings) mesh.SetBlendShapeWeight(index,100*weight);
        }

        public void Reset()
        {
            foreach (string name in channels.Keys) Set(name,0);
        }
    }
}
