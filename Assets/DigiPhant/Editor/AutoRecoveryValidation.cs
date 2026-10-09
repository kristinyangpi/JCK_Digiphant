using System;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace DigiPhant.Editor {
public static class AutoRecoveryValidation {
 const string Report="Logs/AutoRecoveryValidation.txt";
 [MenuItem("DigiPhant/Validate Automatic Recovery")]
 public static void Run(){
  GameObject go=null;
  try{
   go=new GameObject("Automatic recovery test"){hideFlags=HideFlags.HideAndDontSave};
   var c=go.AddComponent<DigiPhantController>();c.performerCount=3;
   var n=go.AddComponent<DigiPhantDriverNavigation>();n.enableDriverNavigation=true;n.usePalmSizeSteering=true;
   void Check(bool ok,string why){if(!ok)throw new Exception(why);}
   string epoch="auto-test";
   void Packet(float t,float h=-1,bool visible=true,float l=1,float right=1){
    var p=new PerformerFrame{slot=1,values=new[]{h,h,0f,0f,0f,0f},confidence=new[]{1f,1f,1f,1f,1f,1f},handDepth=new[]{0f,0f},handDepthConfidence=new[]{1f,1f},palmFacing=new[]{l,right},palmConfidence=new[]{1f,1f},handScale=new[]{.2f,.2f},handScaleConfidence=new[]{1f,1f}};
    Check(c.AcceptPacket(JsonUtility.ToJson(new PoseFrame{version=1,performerCount=3,upperBodyOnly=c.upperBodyOnly,trackingEpoch=epoch,people=visible?new[]{p}:Array.Empty<PerformerFrame>()}),t),"packet");
   }
   Packet(1);Check(n.CalibrateAt(1),"initial calibration");
   Packet(2,0);n.TryRead(2,out _,out _);Packet(2.5f,0);Check(n.TryRead(2.5f,out float f,out _)&&f>0,"initial forward");
   Packet(3,visible:false);Check(!n.TryRead(3,out f,out _)&&f==0&&n.IsCalibrated,"immediate stop");
   Packet(100,visible:false);Check(!n.TryRead(100,out _,out _)&&n.IsCalibrated,"must not expire after long loss");
   Packet(101,0,l:1,right:-1);Check(!n.TryRead(101,out _,out _),"mixed palms must not recover");
   Packet(102,0);Check(!n.TryRead(102,out _,out _),"stability starts");
   Packet(102.3f,visible:false);Check(!n.TryRead(102.3f,out _,out _),"interruption resets stability");
   Packet(103,0);n.TryRead(103,out _,out _);Packet(103.3f,0);Check(!n.TryRead(103.3f,out _,out _),"stability too short");
   Packet(103.6f,0);Check(!n.TryRead(103.6f,out f,out _)&&f==0&&n.IsArmed,"automatic rearm without lowering");
   Packet(104,0);Check(!n.TryRead(104,out _,out _),"gear dwell after recovery");
   Packet(104.4f,0);Check(n.TryRead(104.4f,out f,out _)&&f>0,"automatic forward resumes");
   epoch="new-person-assignment";Packet(105,0);Check(!n.IsCalibrated,"explicit identity reset still clears");
   File.WriteAllText(Report,"PASS: immediate loss stop; no timeout; automatic raised-hand recovery after 0.5s stable data; interrupted/mixed-hand recovery blocked; gear dwell; explicit epoch reset. No scene changed.");
   Debug.Log("AUTO_RECOVERY_VALIDATION_OK");
  }catch(Exception e){File.WriteAllText(Report,"FAIL: "+e);Debug.LogException(e);}
  finally{if(go)UnityEngine.Object.DestroyImmediate(go);}
 }
}}
