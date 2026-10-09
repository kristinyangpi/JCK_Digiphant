using UnityEngine;
namespace StudentWork.CandyWonderland {
 public class CandyTitleFloat : MonoBehaviour {
  public RectTransform panel;
  Vector2 rest;
  void Start(){if(panel)rest=panel.anchoredPosition;}
  void Update(){if(panel)panel.anchoredPosition=rest+Vector2.up*(Mathf.Sin(Time.unscaledTime*1.8f)*7);}
 }
}
