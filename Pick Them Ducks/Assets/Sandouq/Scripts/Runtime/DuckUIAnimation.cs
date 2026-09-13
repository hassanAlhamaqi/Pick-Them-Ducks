using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
namespace Sandouq.Ducks {
 [DisallowMultipleComponent,RequireComponent(typeof(Button))]
 public sealed class DuckUIAnimation:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISubmitHandler {
  [Range(1,1.3f)] public float hoverScale=1.045f,selectedScale=1.07f;
  [Range(.7f,1)] public float pressedScale=.95f;
  public float duration=.14f;
  Vector3 baseline;Button button;Tween tween;bool hovered,pressed,selected;
  void Awake(){baseline=transform.localScale;button=GetComponent<Button>();}
  float Target=>pressed?pressedScale:selected?selectedScale:hovered?hoverScale:1;
  void Animate(){if(!isActiveAndEnabled)return;tween?.Kill();tween=transform.DOScale(baseline*Target,duration).SetEase(Ease.OutBack).SetUpdate(true);}
  public void SetSelected(bool value){if(selected==value)return;selected=value;if(!isActiveAndEnabled)return;tween?.Kill();if(value)tween=DOTween.Sequence().Append(transform.DOScale(baseline*(selectedScale+.04f),duration).SetEase(Ease.OutQuad)).Append(transform.DOScale(baseline*Target,duration).SetEase(Ease.OutBack)).SetUpdate(true);else Animate();}
  public void OnPointerEnter(PointerEventData e){if(!button.interactable)return;hovered=true;Animate();}
  public void OnPointerExit(PointerEventData e){hovered=pressed=false;Animate();}
  public void OnPointerDown(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left||!button.interactable)return;pressed=true;Animate();}
  public void OnPointerUp(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;pressed=false;Animate();}
  public void OnSubmit(BaseEventData e){if(!button.interactable)return;tween?.Kill();tween=DOTween.Sequence().Append(transform.DOScale(baseline*pressedScale,duration*.5f)).Append(transform.DOScale(baseline*Target,duration).SetEase(Ease.OutBack)).SetUpdate(true);}
  void OnDisable(){tween?.Kill();hovered=pressed=false;transform.localScale=baseline;}
  void OnDestroy(){tween?.Kill();}
 }
}
