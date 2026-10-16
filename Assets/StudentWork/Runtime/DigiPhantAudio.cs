using System.Collections.Generic;
using UnityEngine;

namespace StudentWork {
    public class DigiPhantAudio : MonoBehaviour {
        [Range(0,1)] public float musicVolume=.18f, effectsVolume=.7f;
        static DigiPhantAudio instance;
        AudioSource music, effects;
        readonly Dictionary<string,AudioSource> loops=new Dictionary<string,AudioSource>();
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetInstance(){instance=null;}
        public static DigiPhantAudio Ensure(){
            if(!Application.isPlaying || Application.isBatchMode)return null;
            if(!instance)instance=FindAnyObjectByType<DigiPhantAudio>();
            if(!instance)instance=new GameObject("DigiPhant music and effects").AddComponent<DigiPhantAudio>();
            return instance;
        }
        AudioClip Clip(string name){
            if(!clips.TryGetValue(name,out var clip)){
                clip=Resources.Load<AudioClip>("DigiPhantAudio/"+name);clips[name]=clip;
                if(!clip)Debug.LogWarning("Missing DigiPhant sound: "+name);
            }
            return clip;
        }
        AudioSource Source(bool looping,int priority){
            var source=gameObject.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=looping;source.spatialBlend=0;source.priority=priority;
            return source;
        }
        void Awake(){
            instance=this;music=Source(true,200);effects=Source(false,64);
            music.clip=Clip("casual-music");music.volume=musicVolume;
            if(music.clip)music.Play();
        }
        void Update(){music.volume=musicVolume;effects.volume=1;foreach(var source in loops.Values)source.volume=effectsVolume;}
        public static void Play(string name){var audio=Ensure();if(!audio)return;var clip=audio.Clip(name);if(clip)audio.effects.PlayOneShot(clip,audio.effectsVolume);}
        public static void StartLoop(string name){
            var audio=Ensure();if(!audio)return;
            if(!audio.loops.TryGetValue(name,out var source)){
                source=audio.Source(true,80);source.clip=audio.Clip(name);audio.loops[name]=source;
            }
            source.volume=audio.effectsVolume;if(source.clip&&!source.isPlaying)source.Play();
        }
        public static void StopLoop(string name){if(instance&&instance.loops.TryGetValue(name,out var source))source.Stop();}
        void OnDisable(){if(music)music.Stop();if(effects)effects.Stop();foreach(var source in loops.Values)source.Stop();}
    }
}
