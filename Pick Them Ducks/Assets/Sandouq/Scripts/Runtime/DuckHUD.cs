using UnityEngine;
using UnityEngine.UI;
namespace Sandouq.Ducks
{
 public sealed class DuckHUD:MonoBehaviour
 {
  DuckGame game;
  [SerializeField] RectTransform root,journal;[SerializeField] Text money,total,carry,percent,left,prompt,notice,subtitle,diagnostics;[SerializeField] Image bagFill,ring,hold;
  [SerializeField] Button openButton,closeButton,freshButton,backButton;
  public Color selectionColor=new Color(.98f,.75f,.004f); public Color selectionTextColor=new Color(.26f,.20f,.09f);Color[] slotColors,tabColors;Color[] slotTextColors;
  public bool HasAuthoredLayout=>root!=null;
  [SerializeField] Text[] titles=new Text[5],values=new Text[5],costs=new Text[5],slotText=new Text[5];[SerializeField] Button[] buys=new Button[5],slots=new Button[5],tabs=new Button[3];int page;bool reset;float next; bool fifthRowEnabled; Text[] buyLabels=new Text[5];
  readonly int[] order={0,3,1,2,4};readonly string[] toolNames={"HANDS","ROLLER","VACUUM","SWEEPER","ROLLER CAR"};
  public void Initialize(DuckGame owner)
  {
   game=owner;if(root==null){Debug.LogError("Assign the authored Duck HUD prefab.");enabled=false;return;}Bind();Refresh();
  }
  void Bind()
  {
   fifthRowEnabled=titles[4].transform.parent.gameObject.activeSelf;
   for(int i=0;i<5;i++)buyLabels[i]=buys[i].GetComponentInChildren<Text>(true);
   slotColors=new Color[5];slotTextColors=new Color[5];tabColors=new Color[3];
   for(int i=0;i<5;i++){int index=i;slotColors[i]=slots[i].GetComponent<Image>().color;slotTextColors[i]=slotText[i].color;slots[i].onClick.RemoveAllListeners();slots[i].onClick.AddListener(()=>game.Equip(index));buys[i].onClick.RemoveAllListeners();buys[i].onClick.AddListener(()=>Buy(index));}
   for(int i=0;i<3;i++){int index=i;tabColors[i]=tabs[i].GetComponent<Image>().color;tabs[i].onClick.RemoveAllListeners();tabs[i].onClick.AddListener(()=>{page=index;Refresh();});}
   openButton.onClick.RemoveAllListeners();openButton.onClick.AddListener(()=>game.SetMenu(true,true));closeButton.onClick.RemoveAllListeners();closeButton.onClick.AddListener(()=>game.SetMenu(false));backButton.onClick.RemoveAllListeners();backButton.onClick.AddListener(()=>game.SetMenu(false));
   freshButton.onClick.RemoveAllListeners();freshButton.onClick.AddListener(()=>{if(reset)game.StartFresh();else{reset=true;subtitle.text="Reset all progress? Click START FRESH again to confirm.";}});
  }
  void Buy(int row){if(page==0&&row<4)game.BuyUpgrade(row);else if(page==1){if(row<4){int index=order[row+1];if(game.Progress.Data.owned[index])game.BuyToolUpgrade(index);else game.BuyTool(index);}else game.BuyCasket();}Refresh();}
  void Update(){if(game==null)return;hold.fillAmount=game.MenuOpen||game.Progress.Tool!=DuckTool.Hands?0:game.PickupCooldownProgress;if(Time.unscaledTime>=next){next=Time.unscaledTime+.1f;Refresh();}}
  public void Refresh()
  {
   if(game==null||money==null||game.Progress==null)return;var p=game.Progress;var d=p.Data;money.text=d.money.ToString("N0");total.text=d.deposited.ToString("N0");carry.text=d.carried+" <size=23>/ "+p.Capacity+"</size>";bagFill.rectTransform.sizeDelta=new Vector2(256f*d.carried/p.Capacity,12);ring.fillAmount=(float)d.deposited/d.total;percent.text=(100f*d.deposited/d.total).ToString("F0")+"%";left.text=game.Population.Remaining.ToString("N0")+" ducks left";prompt.text=game.MenuOpen?"":game.Prompt;notice.text=game.MenuOpen?"":game.Notice;
   for(int i=0;i<5;i++){slots[i].GetComponent<Image>().color=d.currentTool==i?selectionColor:slotColors[i];slotText[i].color=d.currentTool==i?selectionTextColor:slotTextColors[i];slotText[i].text=toolNames[i]+(d.owned[i]?"":"\nLOCKED");slots[i].interactable=d.owned[i];}
   diagnostics.gameObject.SetActive(game.Diagnostics);diagnostics.text=(1000/Mathf.Max(1,game.SmoothedFrameMs)).ToString("F0")+" FPS / "+game.Population.VisibleInstances+" visible / "+game.Population.DrawCalls+" batches";journal.gameObject.SetActive(game.MenuOpen);if(!game.MenuOpen){reset=false;return;}
   for(int i=0;i<3;i++)tabs[i].GetComponent<Image>().color=page==i?selectionColor:tabColors[i];if(!reset)subtitle.text=d.money.ToString("N0")+" coins / "+p.Equipment.name+" / "+d.casketKits+" casket kits";
   titles[4].transform.parent.gameObject.SetActive(fifthRowEnabled&&page!=0);
   for(int i=0;i<5;i++)
   {
    if(buyLabels[i]!=null)buyLabels[i].text="BUY";
    buys[i].gameObject.SetActive(page!=2);costs[i].text="";values[i].rectTransform.sizeDelta=new Vector2(page==2?740:395,60);
    if(page==0&&i<4){var def=game.Settings.upgrades[i];int level=d.levels[i];titles[i].text=new[]{"Pickup Amount","Pickup Speed","Bag Capacity","Walk / Sprint Speed"}[i];string v=i==0?p.PickupAmount+" > "+(p.PickupAmount+1)+" ducks":i==1?p.Interval.ToString("F2")+" > "+(p.Equipment.interval/(1+(level+1)*def.amount)).ToString("F2")+" s":i==2?p.Capacity+" > "+(p.Capacity+(int)def.amount)+" ducks":(game.Settings.walkSpeed*p.MovementMultiplier).ToString("F1")+" > "+(game.Settings.walkSpeed*(1+(level+1)*def.amount)).ToString("F1")+" m/s";values[i].text="Lv "+level+"/"+def.maxLevel+"    "+(level>=def.maxLevel?"MAXED":v);costs[i].text="$ "+def.Cost(level);buys[i].interactable=level<def.maxLevel&&d.money>=def.Cost(level);}
    else if(page==0){buys[i].interactable=false;}
    else if(page==1&&i<4){int index=order[i+1];var def=game.Settings.tools[index];titles[i].text=def.name;
     if(!d.owned[index]){values[i].text="Shared bag floor: "+def.capacity;costs[i].text="$ "+def.cost;buys[i].interactable=d.money>=def.cost;}
     else if(p.CanUpgrade(index)){int level=d.toolLevels[index];bool maxed=level>=6;values[i].text="Lv "+level+"/6 / "+(maxed?"MAXED":index==3?"Increase sweeping width":"Increase width and speed");costs[i].text=maxed?"MAXED":"$ "+p.UpgradeCost(index);buys[i].interactable=!maxed&&d.money>=p.UpgradeCost(index);if(buyLabels[i]!=null)buyLabels[i].text=maxed?"MAXED":"UPGRADE";}
     else{values[i].text="OWNED / Equip from the tool bar";costs[i].text="";buys[i].interactable=false;if(buyLabels[i]!=null)buyLabels[i].text="OWNED";}
    }
    else if(page==1){titles[i].text="Duck Casket";values[i].text="Install a deposit station / F";costs[i].text="$ "+game.Settings.casketCost;buys[i].interactable=d.money>=game.Settings.casketCost;}
    else{titles[i].text=new[]{"Move & explore","Bushes & trees","Lake ducks","Roller collection","Deposit & build"}[i];values[i].text=new[]{"WASD / Shift sprint / Space jump","LMB / E breaks bushes or shakes trees","Jump across platforms to reach lake ducks","Hold LMB; release to pull your pile in","E deposits / RMB throws / F installs kits"}[i];}
   }
  }
 }
}
