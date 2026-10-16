using System;
using UnityEditor;
using UnityEngine;
namespace StudentWork.Editor {
 public static class GameAudioValidation {
  [MenuItem("DigiPhant/Validate game audio")]
  public static void Validate(){
   foreach(string name in new[]{"casual-music","eat-watermelon","eat-lollipop","drink","splash","eat-log","finish"}){
    var clip=Resources.Load<AudioClip>("DigiPhantAudio/"+name);
    if(!clip||clip.length<.5f||clip.channels!=2)throw new Exception("Audio missing or invalid: "+name);
   }
   if(Application.isPlaying){
    var audio=DigiPhantAudio.Ensure();bool playing=false;
    foreach(var source in audio.GetComponents<AudioSource>())
      if(source.clip&&source.clip.name=="casual-music"&&source.loop&&source.isPlaying)playing=true;
    if(!playing)throw new Exception("Background music did not start in Play mode");
   }
   Debug.Log("GAME_AUDIO_VALIDATION_OK: music and all six event effects imported");
  }
 }
}
