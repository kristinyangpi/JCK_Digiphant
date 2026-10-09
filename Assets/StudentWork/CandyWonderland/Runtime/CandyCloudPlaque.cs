using UnityEngine;
using UnityEngine.UI;
namespace StudentWork.CandyWonderland {
 // Reuses the existing CanvasGroup/title: a soft frosting cloud behind the text.
 [ExecuteAlways,RequireComponent(typeof(CanvasRenderer))] public class CandyCloudPlaque : MaskableGraphic {
  public RectTransform textArea;
  protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var rect=rectTransform.rect;float width=Mathf.Min(rect.width,textArea?textArea.rect.width+90:rect.width);float height=rect.height;for(int i=0;i<7;i++){float x=(i/6f-.5f)*width*.72f;float y=(i%2==0?.08f:-.08f)*height;float rx=width*.22f;float ry=height*(i%2==0?.49f:.43f);Ellipse(vh,new Vector2(x,y),new Vector2(rx,ry));}}
  void Ellipse(VertexHelper vh,Vector2 center,Vector2 radius){int first=vh.currentVertCount;var solid=color;solid.a*=.96f;vh.AddVert(center,solid,Vector2.zero);const int sides=48;for(int ring=0;ring<2;ring++)for(int j=0;j<=sides;j++){float a=j*Mathf.PI*2/sides;var c=solid;if(ring==1)c.a=0;var p=center+new Vector2(Mathf.Cos(a)*radius.x,Mathf.Sin(a)*radius.y)*(ring==0?.92f:1);vh.AddVert(p,c,Vector2.zero);}for(int j=0;j<sides;j++){vh.AddTriangle(first,first+1+j,first+2+j);int a=first+1+j,b=a+1,c=a+sides+1,d=c+1;vh.AddTriangle(a,c,b);vh.AddTriangle(b,c,d);}}
 }
}
