using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DigiPhant;
using static StudentWork.CandyWonderland.Editor.CandyMeshKit;
namespace StudentWork.CandyWonderland.Editor {
 public static class MagicalPondSetup {
  [MenuItem("DigiPhant/Candy Wonderland/Install magical pond in open scene")]
  public static void Install(){
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before installing pond");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(string.IsNullOrEmpty(scene.path))throw new InvalidOperationException("Save your scene first");
   var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();var world=all.Select(t=>t.GetComponent<CandyWorld>()).Single(w=>w);var loco=all.Select(t=>t.GetComponent<DigiPhantLocomotion>()).Single(w=>w);
   if(loco.GetComponent<MagicalPond>())throw new InvalidOperationException("Pond already installed; preserve configured pond");
   var balls=all.Select(t=>t.GetComponent<RollingCandyBall>()).Where(b=>b&&b.gameObject.activeInHierarchy).OrderBy(b=>Nearest(world,(b.start+b.end)*.5f)).ToArray();if(balls.Length!=3)throw new InvalidOperationException("Expected three current referenced candy hazards");var second=balls[1];int station=Nearest(world,(second.start+second.end)*.5f);if(station<=Nearest(world,(balls[0].start+balls[0].end)*.5f))throw new Exception("Hazard route ordering ambiguous");
   string backup=AssetDatabase.GenerateUniqueAssetPath(scene.path.Replace(".unity","_BeforeMagicalPond.unity"));EditorSceneManager.SaveScene(scene,backup,true);
   var host=new GameObject("Magical pond - second hazard replacement");host.transform.SetParent(world.transform,false);host.transform.position=world.route[station]+Vector3.up*.07f;var direction=world.route[station+1]-world.route[station-1];direction.y=0;host.transform.rotation=Quaternion.LookRotation(direction);
   var rim=Mat("Pond pink frosting",new Color(1,.76f,.83f),.77f);var dry=Mat("Pond dry biscuit bed",new Color(1,.82f,.79f),.6f);var bed=Primitive("Walkable candy pond bed",host.transform,PrimitiveType.Sphere,new Vector3(0,-.12f,0),new Vector3(10.5f,.27f,5.5f),dry);
   var waterMat=ShaderMat("Magical turquoise pond","StudentWork/CandyWonderland/Magical Pond");var surface=Part("Shimmering pond water",host.transform,Save("Rounded magical pond surface",Disk()),waterMat,new Vector3(0,.19f,0));surface.transform.localScale=new Vector3(5.25f,1,2.7f);
   // The existing trail MeshCollider remains the walkable surface after draining.
   // Collision gate spans the fence opening and capped flight height.
   var gate=host.AddComponent<BoxCollider>();gate.center=new Vector3(0,5,0);gate.size=new Vector3(11.3f,12,4.6f);
   var movement=loco.GetComponent<StudentObstacleMovement>();movement.obstacles=movement.obstacles.Where(b=>b!=second.solid).Concat(new[]{gate}).ToArray();second.gameObject.SetActive(false);
   // Keep this exact previous obstacle in the backup and as inactive rollback data.
   foreach(int sign in new[]{-1,1})for(int i=0;i<13;i++){float x=-5.1f+i*.85f;Primitive("Rounded frosting shore",host.transform,PrimitiveType.Sphere,new Vector3(x,.1f,sign*2.64f),new Vector3(.95f,.25f,.35f),rim);}
   foreach(int sign in new[]{-1,1})for(int i=0;i<5;i++)Primitive("Pastel shore pebble",host.transform,PrimitiveType.Sphere,new Vector3(sign*5.25f,.23f,-2+i),new Vector3(.4f,.35f,.55f),Mat("Pond pearl",new Color(.78f,.88f,1),.8f));
   var pond=loco.gameObject.AddComponent<MagicalPond>();pond.world=world;pond.locomotion=loco;pond.controller=loco.GetComponent<DigiPhantController>();pond.pickup=loco.GetComponent<LollipopPickup>();pond.pond=host.transform;pond.water=surface.transform;pond.barrier=gate;pond.streamMaterial=ShaderMat("Pond luminous stream","StudentWork/CandyWonderland/Pond Magic");pond.rainbowMaterial=pond.streamMaterial;pond.sparkleMaterial=pond.streamMaterial;
   RefineAtmosphere(world,loco);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("MAGICAL_POND_INSTALLED: reference "+second.name+" route station="+station+" replaced; first/third hazards preserved. Backup="+backup);
  }
  static int Nearest(CandyWorld world,Vector3 p){return Enumerable.Range(0,world.route.Length).OrderBy(i=>new Vector2(world.route[i].x-p.x,world.route[i].z-p.z).sqrMagnitude).First();}
  static Material ShaderMat(string name,string shader){var path=Art+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat)return mat;var sh=Shader.Find(shader);if(!sh)throw new Exception("Missing pond shader: "+shader);mat=new Material(sh){name=name};AssetDatabase.CreateAsset(mat,path);return mat;}
  static Mesh Disk(){const int n=96;var v=new System.Collections.Generic.List<Vector3>{Vector3.zero};var t=new System.Collections.Generic.List<int>();var uv=new System.Collections.Generic.List<Vector2>{new Vector2(.5f,.5f)};for(int i=0;i<=n;i++){float a=i*Mathf.PI*2/n;v.Add(new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)));uv.Add(new Vector2(Mathf.Cos(a)*.5f+.5f,Mathf.Sin(a)*.5f+.5f));if(i>0)t.AddRange(new[]{0,i+1,i});}return Build(v,t,uv);}
  static void RefineAtmosphere(CandyWorld world,DigiPhantLocomotion loco){
   foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){light.shadowStrength=.32f;light.shadows=LightShadows.Soft;light.intensity=1.06f;light.color=new Color(1,.96f,.91f);}
   var data=loco.followCamera.GetUniversalAdditionalCameraData();data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;RenderSettings.fogDensity=.0065f;
   var volume=world.GetComponentInChildren<Volume>();if(volume){var profile=volume.sharedProfile;if(!profile.TryGet<ColorAdjustments>(out var color))color=profile.Add<ColorAdjustments>(true);color.contrast.Override(-5);color.saturation.Override(-3);EditorUtility.SetDirty(profile);}
   // Existing animated waterfall curtains are preserved. Add lightweight edge foam
   // and bounded moving droplets, never collision or expensive water simulation.
   var hosts=world.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Animated waterfall ")).ToArray();var mat=ShaderMat("Waterfall magic droplets","StudentWork/CandyWonderland/Pond Magic");foreach(var host in hosts){var r=host.GetComponent<MeshRenderer>();if(!r)continue;var g=new GameObject("Waterfall edge droplets and mist");g.transform.SetParent(host,false);var ps=g.AddComponent<ParticleSystem>();var main=ps.main;main.loop=true;main.startLifetime=3.5f;main.startSpeed=2;main.startSize=new ParticleSystem.MinMaxCurve(.035f,.09f);main.maxParticles=40;main.startColor=new Color(.66f,.91f,1,.5f);main.gravityModifier=.06f;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(r.bounds.size.x,.12f,.2f);shape.rotation=new Vector3(90,0,0);var em=ps.emission;em.rateOverTime=7;g.GetComponent<ParticleSystemRenderer>().sharedMaterial=mat;}
  }
 }
}
