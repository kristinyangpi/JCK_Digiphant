using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace StudentWork {
 [DisallowMultipleComponent] public class LevelIntroduction : MonoBehaviour {
  public CanvasGroup group;
  public Text title;
  public DigiPhant.DigiPhantController existingUI;
  public float fadeInSeconds=.45f,holdSeconds=3,fadeOutSeconds=.55f;
  public float Elapsed {get;private set;}
  public float Duration=>fadeInSeconds+holdSeconds+fadeOutSeconds;
  public bool Visible=>group&&group.gameObject.activeSelf&&group.alpha>0;
  void OnEnable(){if(Application.isPlaying)Replay();else if(group)group.alpha=0;}
  void Start(){Replay();}
  public void Replay(){Elapsed=0;SetElapsed(0);}
  void Update(){if(Application.isPlaying&&Elapsed<=Duration)SetElapsed(Elapsed+Time.unscaledDeltaTime);}
  public void RefreshLayout(){
   if(!title)return;var canvas=GetComponent<Canvas>();if(!canvas||canvas.pixelRect.width<=0)return;
   if(!existingUI)existingUI=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DigiPhant.DigiPhantController>(true)).FirstOrDefault();
   float width=canvas.pixelRect.width;float sidebar=existingUI&&existingUI.showControls?Mathf.Min(350,width*.32f)+20:0;
   float safePixels=Mathf.Max(40,Mathf.Min(width*.76f,(width*.5f-sidebar-10)*2));float half=safePixels/Mathf.Max(.0001f,canvas.scaleFactor)*.5f;
   var rect=title.rectTransform;rect.anchorMin=new Vector2(.5f,0);rect.anchorMax=new Vector2(.5f,1);rect.offsetMin=new Vector2(-half,20);rect.offsetMax=new Vector2(half,-20);
  }
  public void SetElapsed(float elapsed){
   Elapsed=Mathf.Max(0,elapsed);if(!group)return;RefreshLayout();
   group.blocksRaycasts=false;group.interactable=false;
   bool visible=Elapsed<Duration;group.gameObject.SetActive(visible);
   float opacity=Elapsed<fadeInSeconds?Elapsed/Mathf.Max(.01f,fadeInSeconds):Elapsed<fadeInSeconds+holdSeconds?1:1-(Elapsed-fadeInSeconds-holdSeconds)/Mathf.Max(.01f,fadeOutSeconds);
   group.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01(opacity));
  }
 }
}
