using System;
using UnityEngine;
using DigiPhant;
namespace StudentWork.Editor {
public static class P2HeadInputValidation {
 public static void Run() {
  var go = new GameObject("P2 head input validation");
  try {
   var c = go.AddComponent<DigiPhantController>();
   var role = go.AddComponent<P2EatingRole>();
   c.inputMode=InputMode.Camera; c.performerCount=3;
   void Check(bool ok,string why) { if(!ok) throw new Exception("P2 head: "+why); }
   string Packet(float pitch=20,float confidence=1,int slot=2) => JsonUtility.ToJson(new PoseFrame {
    version=1,performerCount=3,people=new[]{new PerformerFrame {slot=slot,headPitchDegrees=pitch,headConfidence=confidence,
    handDepth=new float[2],handDepthConfidence=new float[2],palmFacing=new float[2],palmConfidence=new float[2],handScale=new float[2],handScaleConfidence=new float[2],flap=new float[4],values=new float[6],confidence=new[]{1f,1f,1f,1f,1f,1f}}}});
   void Accept(string json,float now) => Check(c.AcceptPacket(json,now),"valid packet rejected");
   Accept(Packet(),1); Check(c.TryReadHeadPitch(2,1,out var pitch)&&pitch==20,"valid head missing");
   Check(!c.TryReadHeadPitch(1,1,out _)&&!c.TryReadHeadPitch(3,1,out _),"wrong slot accepted");
   Check(!c.TryReadHeadPitch(2,2,out _)&&!c.TryReadHeadPitch(2,0,out _),"stale/future timestamp accepted");
   Check(!c.TryReadHeadPitch(2,float.NaN,out _),"NaN time accepted");
   Accept(Packet(confidence:0),2); Check(!c.TryReadHeadPitch(2,2,out _),"zero confidence accepted");
   Accept(Packet(),3); Accept(Packet(slot:1),3.1f); Check(!c.TryReadHeadPitch(2,3.1f,out _),"absent P2 retained");
   Accept(Packet(),4); Accept(Packet().Replace("\"headPitchDegrees\":20.0,", "").Replace("\"headConfidence\":1.0,", ""),4.1f);
   // Explicit legacy packet has neither head field, irrespective of JsonUtility number formatting.
   Accept("{\"version\":1,\"performerCount\":3,\"people\":[{\"slot\":2,\"values\":[0,0,0,0,0,0],\"confidence\":[1,1,1,1,1,1]}]}",4.2f);
   Check(!c.TryReadHeadPitch(2,4.2f,out _),"legacy packet retained head");
   Accept(Packet(),5); role.enableFixedRoles=false; Check(!c.TryReadHeadPitch(2,5,out _),"disabled role accepted");role.enableFixedRoles=true;
   role.enabled=false;Check(!c.TryReadHeadPitch(2,5,out _),"disabled component accepted");role.enabled=true;
   c.inputMode=InputMode.TestSliders;Check(!c.TryReadHeadPitch(2,5,out _),"slider mode accepted");c.inputMode=InputMode.Camera;
   c.performerCount=1;Check(!c.TryReadHeadPitch(2,5,out _),"solo mode accepted");c.performerCount=3;
   Check(!c.AcceptPacket(Packet(91),6),"out of range pitch accepted");
   Check(!c.TryReadHeadPitch(2,6,out _),"rejected packet retained head");
   Check(!c.AcceptPacket(Packet(confidence:2),7),"invalid confidence accepted");
   string invalid=Packet().Replace("\"headPitchDegrees\":20.0","\"headPitchDegrees\":NaN").Replace("\"headPitchDegrees\":20,","\"headPitchDegrees\":NaN,");
   Check(invalid.Contains("NaN"),"NaN fixture construction failed");
   Check(!c.AcceptPacket(invalid,8),"NaN pitch accepted");
   Debug.Log("P2_HEAD_INPUT_VALIDATION_OK");
  } finally { UnityEngine.Object.DestroyImmediate(go); }
 }
}}
