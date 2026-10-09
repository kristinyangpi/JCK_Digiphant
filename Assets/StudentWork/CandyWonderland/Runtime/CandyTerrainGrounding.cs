using System;
using System.Reflection;
using UnityEngine;
using DigiPhant;
namespace StudentWork.CandyWonderland {
 // Optional scene adapter. The established locomotion/flight scripts remain byte-for-byte intact.
 // Their ground reference is currently private and flat; bind that one field explicitly,
 // rather than replacing controllers, inputs, animation evaluation or camera following.
 [DefaultExecutionOrder(100)]
 public class CandyTerrainGrounding : MonoBehaviour {
  public CandyWorld world;
  public DigiPhantLocomotion locomotion;
  FieldInfo groundReference;
  StudentFlight flight;FinishBallet ballet;
  float originalGround;bool initialized;
  public float CurrentGround {get;private set;}
  public bool Ready=>initialized;
  void Start(){Initialize();}
  public void Initialize(){
   if(initialized)return;
   if(!world||!locomotion||!locomotion.travelRoot)throw new InvalidOperationException("Candy terrain grounding references missing");
   if(!locomotion.Initialise())throw new InvalidOperationException("Existing elephant locomotion is not ready");
   groundReference=typeof(DigiPhantLocomotion).GetField("startPosition",BindingFlags.Instance|BindingFlags.NonPublic);
   if(groundReference==null||groundReference.FieldType!=typeof(Vector3))throw new InvalidOperationException("Locomotion ground reference changed; terrain adapter requires review");
   originalGround=((Vector3)groundReference.GetValue(locomotion)).y;
   flight=GetComponent<StudentFlight>();ballet=GetComponent<FinishBallet>();initialized=true;ApplyGround();
  }
  void Update(){if(initialized&&(!ballet||!ballet.ControlsElephant))ApplyGround();}
  public void GroundNow(){Initialize();ApplyGround();}
  void ApplyGround(){
   var root=locomotion.travelRoot;CurrentGround=world.GroundHeight(root.position)+originalGround+.085f;
   var start=(Vector3)groundReference.GetValue(locomotion);start.y=CurrentGround;groundReference.SetValue(locomotion,start);
   float y=CurrentGround+(flight?flight.HeightOffset:0);float change=y-root.position.y;
   root.position+=Vector3.up*change;
   if(locomotion.followElephant&&locomotion.followCamera)locomotion.followCamera.transform.position+=Vector3.up*change;
  }
  void LateUpdate(){if(initialized&&(!ballet||!ballet.ControlsElephant))ApplyGround();}
  void OnDisable(){if(initialized&&locomotion){var start=(Vector3)groundReference.GetValue(locomotion);start.y=originalGround;groundReference.SetValue(locomotion,start);}initialized=false;}
 }
}
