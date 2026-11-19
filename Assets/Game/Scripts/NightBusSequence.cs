using System;
using System.Collections;
using UnityEngine;

namespace GordonMann
{
    public sealed class NightBusSequence : MonoBehaviour
    {
        public Transform actor,bus,busArrival,busStop,busDeparture,curb,step,inside,leftDoor,rightDoor,coin,fareBox;
        public Transform streetShot,boardingShot;
        public Transform[] wheels;
        public GameObject heading,narration;
        public AudioSource speaker;
        public AudioClip engine,brake,doorSound,fareSound,footfall;
        public bool Playing { get; private set; }

        GordonPoses poses;
        Quaternion[] wheelStarts;
        float wheelTurn;

        void Awake()
        {
            poses=new GordonPoses(actor);
            wheelStarts=Array.ConvertAll(wheels,wheel=>wheel.localRotation);
        }

        public bool TryPlay(Choice choice)
        {
            if(choice.label!="Night Bus" || choice.targetScene!="Victory") return false;
            if(Playing) return true;
            Playing=true;
            StartCoroutine(Escape(choice.targetScene));
            return true;
        }

        IEnumerator Escape(string target)
        {
            heading.SetActive(false);
            narration.SetActive(false);
            Shot(streetShot,4.43f);
            yield return Walk(curb.position);
            yield return Over(.24f,t=>poses.Set("WatchDesk",Ease(t)*.62f));
            yield return new WaitForSeconds(.22f);
            speaker.PlayOneShot(engine,.59f);
            yield return MoveBus(busArrival.position,busStop.position,1.74f,t=>1-(1-t)*(1-t));
            speaker.PlayOneShot(brake,.72f);
            yield return new WaitForSeconds(.23f);
            Shot(boardingShot,2.30f);
            speaker.PlayOneShot(doorSound,.66f);
            yield return Doors(true,.48f);
            poses.Set("WatchDesk",0);
            yield return Walk(step.position);
            var coinStart=actor.position+new Vector3(.39f,1.04f,.34f);
            coin.gameObject.SetActive(true);
            yield return Over(.32f,t=>
            {
                poses.Set("Reach",.60f*Ease(t));
                coin.position=Vector3.Lerp(coinStart,fareBox.position,Ease(t))+Vector3.up*(.10f*Mathf.Sin(Mathf.PI*t));
                coin.localRotation=Quaternion.Euler(68,0,130*t);
            });
            coin.gameObject.SetActive(false);
            speaker.PlayOneShot(fareSound,.83f);
            yield return new WaitForSeconds(.16f);
            poses.Set("Reach",0);
            poses.Set("ReadySneak",.52f);
            yield return Walk(inside.position);
            actor.SetParent(bus,true);
            actor.gameObject.SetActive(false);
            speaker.PlayOneShot(doorSound,.63f);
            yield return Doors(false,.42f);
            yield return new WaitForSeconds(.20f);
            Shot(streetShot,4.43f);
            speaker.PlayOneShot(engine,.65f);
            yield return MoveBus(busStop.position,busDeparture.position,1.69f,t=>t*t);
            GameFlow.PickOutcome(target);
        }

        IEnumerator MoveBus(Vector3 from,Vector3 to,float seconds,Func<float,float> ease)
        {
            float turn=wheelTurn;
            float range=Vector3.Distance(from,to)*Mathf.Rad2Deg/.40f;
            yield return Over(seconds,t=>
            {
                float fraction=ease(t);
                bus.position=Vector3.Lerp(from,to,fraction);
                wheelTurn=turn-range*fraction;
                for(int i=0;i<wheels.Length;i++)
                    wheels[i].localRotation=wheelStarts[i]*Quaternion.Euler(0,wheelTurn,0);
            });
        }

        IEnumerator Doors(bool opening,float seconds)
        {
            var a=leftDoor.localPosition;
            var b=rightDoor.localPosition;
            float distance=opening?.73f:-.73f;
            yield return Over(seconds,t=>
            {
                float travel=Ease(t)*distance;
                leftDoor.localPosition=a-Vector3.right*travel;
                rightDoor.localPosition=b+Vector3.right*travel;
            });
        }

        IEnumerator Walk(Vector3 target)
        {
            Vector3 from=actor.position;
            Vector3 direction=target-from;
            direction.y=0;
            var turn=actor.rotation;
            var face=Quaternion.LookRotation(direction);
            yield return Over(.19f,t=>actor.rotation=Quaternion.Slerp(turn,face,Ease(t)));
            int stepCount=Mathf.Max(1,Mathf.RoundToInt(direction.magnitude/.48f));
            for(int i=0;i<stepCount;i++)
            {
                string side=i%2==0?"Right":"Left";
                float begin=i/(float)stepCount;
                float end=(i+1f)/stepCount;
                yield return Over(.33f,t=>
                {
                    actor.position=Vector3.Lerp(from,target,Mathf.Lerp(begin,end,t));
                    poses.Set("Pass"+side,.58f*Mathf.Sin(Mathf.PI*t));
                });
                speaker.PlayOneShot(footfall,.49f);
            }
        }

        static void Shot(Transform marker,float size)
        {
            if(Camera.main is not Camera camera) return;
            camera.transform.SetPositionAndRotation(marker.position,marker.rotation);
            camera.orthographicSize=size;
        }

        static float Ease(float t)=>Mathf.SmoothStep(0,1,t);

        static IEnumerator Over(float seconds,Action<float> frame)
        {
            for(float t=0;t<seconds;t+=Time.deltaTime)
            {
                frame(t/seconds);
                yield return null;
            }
            frame(1);
        }
    }
}
