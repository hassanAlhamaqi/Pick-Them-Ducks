using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Sandouq.Ducks
{
    public sealed class DuckHUD : MonoBehaviour
    {
        DuckGame game;
        Font font;
        RectTransform root, menu;
        RectTransform promptBackdrop, noticeBackdrop;
        Text money, carry, progress, tool, prompt, notice, remaining, menuTitle, menuSubtitle, diagnostics, destination;
        Image carryFill, stageFill;
        GameObject shopContent;
        Button restartButton;
        Text restartLabel, casketLabel; Button casketButton;
        bool confirmRestart;
        readonly Text[] toolLabels = new Text[5], upgradeLabels = new Text[4];
        readonly Button[] toolButtons = new Button[5], upgradeButtons = new Button[4];
        float nextRefresh;
        int lastMoney = -1, lastCarried = -1, lastCapacity = -1, lastDeposited = -1, lastTool = -1, lastRemaining = -1;
        int lastBoxDistance = -1, lastShopDistance = -1;
        static readonly Color Ink = new Color(.025f, .10f, .13f, .96f);
        static readonly Color Gold = new Color(1, .77f, .2f);
        static readonly Color Muted = new Color(.64f, .77f, .77f);
        public void Initialize(DuckGame owner)
        {
            game = owner; font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = new GameObject("Duck HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvas.transform.SetParent(transform);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            root = canvas.GetComponent<RectTransform>();
            if (FindAnyObjectByType<EventSystem>() == null) { var events = new GameObject("UI input", typeof(EventSystem), typeof(InputSystemUIInputModule)); events.transform.SetParent(transform); }
            Panel(root, new Vector2(30, -30), new Vector2(385, 146), Ink);
            Label(root, "PICK THEM DUCKS", 16, Gold, new Vector2(50, -46), new Vector2(330, 28));
            money = Label(root, "$0", 44, Color.white, new Vector2(48, -77), new Vector2(340, 58));
            tool = Label(root, "HANDS", 15, Muted, new Vector2(52, -140), new Vector2(320, 25));
            var right = Panel(root, new Vector2(-30, -30), new Vector2(350, 125), Ink, new Vector2(1, 1), new Vector2(1, 1));
            Label(right, "ONE FIELD. EVERY LAST DUCK.", 14, Gold, new Vector2(20, -15), new Vector2(315, 24));
            progress = Label(right, "", 27, Color.white, new Vector2(20, -44), new Vector2(315, 34));
            var track = Panel(right, new Vector2(20, -90), new Vector2(310, 5), new Color(.15f, .28f, .3f));
            stageFill = Panel(track, Vector2.zero, new Vector2(310, 5), Gold).GetComponent<Image>();
            remaining = Label(root, "", 14, Ink, new Vector2(-30, -166), new Vector2(350, 25), TextAnchor.MiddleRight, new Vector2(1, 1), new Vector2(1, 1));
            var bottom = Panel(root, new Vector2(0, 65), new Vector2(460, 80), Ink, new Vector2(.5f, 0), new Vector2(.5f, 0));
            carry = Label(bottom, "", 24, Color.white, new Vector2(22, -13), new Vector2(420, 34), TextAnchor.MiddleCenter);
            var bar = Panel(bottom, new Vector2(22, -60), new Vector2(416, 6), new Color(.15f, .28f, .3f));
            carryFill = Panel(bar, Vector2.zero, new Vector2(416, 6), Gold).GetComponent<Image>();
            Panel(root, Vector2.zero, new Vector2(1600, 48), Ink, new Vector2(.5f, 0), new Vector2(.5f, 0));
            Label(root, "WASD  move     SHIFT  run     LMB  collect     E  interact     1-5 tools   RMB throw   F casket     ESC  pause", 15, Color.white,
                new Vector2(0, 20), new Vector2(1200, 28), TextAnchor.MiddleCenter, new Vector2(.5f, 0), new Vector2(.5f, 0));
            promptBackdrop = Panel(root, new Vector2(0, -66), new Vector2(760, 45), Ink, new Vector2(.5f, .5f), new Vector2(.5f, 1));
            prompt = Label(root, "", 18, Color.white, new Vector2(0, -70), new Vector2(950, 38), TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(.5f, 1));
            noticeBackdrop = Panel(root, new Vector2(0, 122), new Vector2(620, 54), Ink, new Vector2(.5f, .5f), new Vector2(.5f, 1));
            notice = Label(root, "", 26, Gold, new Vector2(0, 120), new Vector2(1100, 50), TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(.5f, 1));
            Panel(root, Vector2.zero, new Vector2(26, 28), Ink, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            Label(root, "+", 28, Color.white, Vector2.zero, new Vector2(30, 35), TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            Panel(root, new Vector2(20, 65), new Vector2(265, 60), Ink, new Vector2(0, 0), new Vector2(0, 0));
            destination = Label(root, "", 16, Color.white, new Vector2(30, 40), new Vector2(450, 85), TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(0, 0));
            destination.rectTransform.anchoredPosition = new Vector2(35, 76); destination.rectTransform.sizeDelta = new Vector2(235, 42);
            diagnostics = Label(root, "", 15, Color.white, new Vector2(30, -205), new Vector2(620, 150));
            menu = Panel(root, Vector2.zero, new Vector2(820, 780), Ink, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            menu.GetComponent<Image>().color = new Color(.025f, .10f, .13f, 1);
            menuTitle = Label(menu, "THE TOOL SHED", 36, Gold, new Vector2(35, -25), new Vector2(750, 50));
            menuSubtitle = Label(menu, "", 18, Muted, new Vector2(35, -86), new Vector2(745, 58));
            shopContent = new GameObject("Shop rows", typeof(RectTransform)); shopContent.transform.SetParent(menu, false); var rows = shopContent.GetComponent<RectTransform>(); rows.anchorMin = rows.anchorMax = new Vector2(0, 1); rows.pivot = new Vector2(0, 1); rows.anchoredPosition = new Vector2(35, -155);
            for (int i = 0; i < 5; i++)
            {
                int index = i; toolButtons[i] = Button(rows, new Vector2(0, -i * 62), new Vector2(360, 55), () => { if (game.Progress.Data.owned[index]) game.Equip(index); else game.BuyTool(index); Refresh(); }, out toolLabels[i]);
            }
            for (int i = 0; i < 4; i++)
            {
                int index = i; upgradeButtons[i] = Button(rows, new Vector2(380, -i * 76), new Vector2(370, 65), () => { game.BuyUpgrade(index); Refresh(); }, out upgradeLabels[i]);
            }
            casketButton=Button(rows,new Vector2(0,-340),new Vector2(750,75),()=>{game.BuyCasket();Refresh();},out casketLabel);
            Button(menu, new Vector2(35, -704), new Vector2(750, 48), () => game.SetMenu(false), out var close).GetComponent<Image>().color = Gold;
            close.text = "BACK TO THE DUCKS  /  ESC"; close.color = Ink;
            restartButton = Button(menu, new Vector2(35, -630), new Vector2(750, 48), () => {
                if (confirmRestart) game.StartFresh();
                else { confirmRestart = true; restartLabel.text = "RESET MONEY, TOOLS & FIELD?  CLICK AGAIN TO CONFIRM"; }
            }, out restartLabel);
            restartLabel.text = "START A FRESH FIELD";
            menu.gameObject.SetActive(false); Refresh();
        }
        void Update() { if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .1f; Refresh(); } }
        public void Refresh()
        {
            if (game.Progress == null || money == null) return;
            var p = game.Progress; var d = p.Data;
            if (lastMoney != d.money) { money.text = "$" + d.money.ToString("N0") + " <size=18>MONEY</size>"; lastMoney = d.money; }
            if (lastTool != d.currentTool || lastCapacity != p.Capacity) { tool.text = p.Equipment.name + "   /   " + p.Capacity + " CAPACITY"; lastTool = d.currentTool; }
            if (lastCarried != d.carried || lastCapacity != p.Capacity)
            {
                carry.text = "DUCKS: " + d.carried + " / " + p.Capacity + (p.FreeSpace == 0 ? "  •  FULL" : "");
                carryFill.rectTransform.sizeDelta = new Vector2(416f * d.carried / p.Capacity, 6); lastCarried = d.carried; lastCapacity = p.Capacity;
            }
            if (lastDeposited != d.deposited)
            {
                progress.text = d.deposited.ToString("N0") + " / " + d.total.ToString("N0");
                stageFill.rectTransform.sizeDelta = new Vector2(310f * d.deposited / d.total, 5); lastDeposited = d.deposited;
            }
            if (lastRemaining != game.Population.Remaining) { remaining.text = game.Population.Remaining.ToString("N0") + " IN THE FIELD"; lastRemaining = game.Population.Remaining; }
            if(d.casketOwned) remaining.text=game.Population.Remaining.ToString("N0")+" IN FIELD / CASKET "+d.casketDucks.Length+"/1000";
            prompt.text = game.Prompt; notice.text = game.Notice;
            promptBackdrop.gameObject.SetActive(!game.MenuOpen && !string.IsNullOrEmpty(game.Prompt));
            noticeBackdrop.gameObject.SetActive(!game.MenuOpen && !string.IsNullOrEmpty(game.Notice));
            int boxDistance = Mathf.RoundToInt(Vector3.Distance(game.Player.transform.position, game.Stage.BoxPosition));
            int shopDistance = Mathf.RoundToInt(Vector3.Distance(game.Player.transform.position, game.Stage.ShopPosition));
            if (boxDistance != lastBoxDistance || shopDistance != lastShopDistance)
            {
                destination.text = "COLLECTION BOX   " + boxDistance + "m\nTOOL SHOP   " + shopDistance + "m";
                lastBoxDistance = boxDistance; lastShopDistance = shopDistance;
            }
            diagnostics.gameObject.SetActive(game.Diagnostics);
            if (game.Diagnostics) diagnostics.text = "F3  /  RENDER DIAGNOSTICS\n" + (1000 / Mathf.Max(1, game.SmoothedFrameMs)).ToString("F0") + " FPS  •  " + game.SmoothedFrameMs.ToString("F1") + " ms\n" + game.Population.VisibleInstances.ToString("N0") + " visible instances / " + game.Population.DrawCalls + " batches\nSleeping ducks instanced / bounded rolling physics pool";
            menu.gameObject.SetActive(game.MenuOpen);
            if (!game.MenuOpen) { confirmRestart = false; restartLabel.text = "START A FRESH FIELD"; return; }
            bool complete = p.Complete;
            menuTitle.text = complete ? "EVERY DUCK. COLLECTED." : game.ShopOpen ? "THE TOOL SHED" : "TAKE A BREATHER";
            menuSubtitle.text = complete ? "The field is clear. " + d.deposited.ToString("N0") + " ducks safely deposited.\nYour progress has been saved." : game.ShopOpen ? "$" + d.money.ToString("N0") + " available  •  Better tools make lighter work." : "Progress saves automatically.\nWalk to the yellow shop counter and press E to upgrade.";
            shopContent.SetActive(game.ShopOpen && !complete);
            restartButton.gameObject.SetActive(!game.ShopOpen || complete);
            casketLabel.text=d.casketOwned ? "PORTABLE CASKET  /  "+d.casketDucks.Length+" / 1000 DUCKS\nE stash / F carry or drop / carry it to the box to deposit" : "PORTABLE CASKET  /  1,000 DUCKS  /  $450\nStore ducks in the field, then carry the casket back to deposit";
            casketButton.interactable=!d.casketOwned && d.money>=450;
            for (int i = 0; i < 5; i++)
            {
                var def = game.Settings.tools[i];
                toolLabels[i].text = def.name + "  /  " + def.capacity + " capacity\n" + (i == 1 ? "SCOOP 4" : i == 2 || i == 4 ? "VACUUM" : i==3 ? "PUSH DUCKS" : "PICKUP") + "    " + (d.owned[i] ? (d.currentTool == i ? "EQUIPPED" : "EQUIP") : "$" + def.cost);
                toolButtons[i].interactable = d.owned[i] ? d.currentTool != i && d.carried <= def.capacity + d.levels[0] * game.Settings.upgrades[0].amount : d.money >= def.cost;
            }
            for (int i = 0; i < 4; i++)
            {
                var def = game.Settings.upgrades[i]; bool max = d.levels[i] >= def.maxLevel; bool locked = i >= 2 && !d.owned[2] && !d.owned[4];
                upgradeLabels[i].text = def.name + "    LV " + d.levels[i] + "/" + def.maxLevel + "    " + (max ? "MAXED" : locked ? "BUY VACUUM FIRST" : "$" + def.Cost(d.levels[i]) + "  /  UPGRADE");
                upgradeButtons[i].interactable = !max && !locked && d.money >= def.Cost(d.levels[i]);
            }
        }
        RectTransform Panel(Transform parent, Vector2 pos, Vector2 size, Color color, Vector2? anchor = null, Vector2? pivot = null)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(0, 1); rect.pivot = pivot ?? new Vector2(0, 1); rect.anchoredPosition = pos; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return rect;
        }
        Text Label(Transform parent, string text, int size, Color color, Vector2 pos, Vector2 dimensions, TextAnchor align = TextAnchor.UpperLeft, Vector2? anchor = null, Vector2? pivot = null)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(0, 1); rect.pivot = pivot ?? new Vector2(0, 1); rect.anchoredPosition = pos; rect.sizeDelta = dimensions;
            var label = go.GetComponent<Text>(); label.font = font; label.text = text; label.fontSize = size; label.color = color; label.alignment = align; label.raycastTarget = false; label.supportRichText = true;
            return label;
        }
        Button Button(Transform parent, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action, out Text label)
        {
            var rect = Panel(parent, pos, size, new Color(.12f, .27f, .29f)); rect.GetComponent<Image>().raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(.75f, 1, .9f); colors.disabledColor = new Color(.42f, .48f, .48f); button.colors = colors;
            button.onClick.AddListener(action); label = Label(rect, "", 17, Color.white, new Vector2(14, -6), size - new Vector2(28, 12), TextAnchor.MiddleCenter); return button;
        }
    }
}
