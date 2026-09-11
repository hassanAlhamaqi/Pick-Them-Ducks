using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sandouq.Ducks
{
    [DefaultExecutionOrder(-100)]
    public sealed class DuckGame : MonoBehaviour
    {
        public PrototypeSettings Settings;
        public DuckStage authoredStage;
        public DuckPlayer authoredPlayer;
        public DuckPark Park;
        public DuckPhysics Physics { get; private set; }
        public bool CarryingCasket { get; private set; }
        float nextThrow;
        public void BuyCasket() { if(Progress.BuyCasket()) { Park.casket.gameObject.SetActive(true); Save(); ShowNotice("Casket ready beside the shop. E stash / F carry."); } }
        public void ToggleCasket()
        {
            if(!Progress.Data.casketOwned || (!CarryingCasket && !Near(Park.casket.position)))return;
            CarryingCasket=!CarryingCasket;
            Park.casket.SetParent(CarryingCasket ? Player.View.transform : Park.transform, true);
            if(CarryingCasket) { Park.casket.localPosition=new Vector3(0,-.65f,1.25f); Park.casket.localRotation=Quaternion.identity; }
            else { Park.casket.position=Park.Land(Player.transform.position+Player.transform.forward*1.8f); Park.casket.rotation=Quaternion.Euler(0,Player.transform.eulerAngles.y,0); }
            foreach(var c in Park.casket.GetComponentsInChildren<Collider>())c.enabled=!CarryingCasket;
            RefreshTool(); Progress.Touch(); Save();
        }
        public bool ThrowDuck()
        {
            int id=Progress.ThrowId; if(id<0 || CarryingCasket)return false;
            var camera=Player.View.transform;
            if(!Physics.Launch(id,camera.position+camera.forward*.7f,camera.forward*8+Vector3.up*2,true))return false;
            Progress.Thrown(); return true;
        }
        [Tooltip("For automated tests only: never loads or overwrites the player's save.")]
        public bool isolatedTest;
        public Progression Progress { get; private set; }
        public DuckPopulationManager Population { get; private set; }
        public DuckPlayer Player { get; private set; }
        public DuckStage Stage { get; private set; }
        public DuckHUD HUD { get; private set; }
        public bool MenuOpen { get; private set; }
        public bool ShopOpen { get; private set; }
        public bool Diagnostics { get; private set; }
        public string Prompt { get; private set; }
        public string Notice { get; private set; }
        public float SmoothedFrameMs { get; private set; }
        DuckFeedback feedback;
        float nextPickup, nextSave, noticeUntil;
        int pickupBurst;
        float burstUntil;
        Tween boxTween;
        Vector3 boxScale;

        void Start()
        {
            if (Settings == null || Settings.duckPrefab == null) { Debug.LogError("Assign Duck Prototype Settings with the supplied duck prefab."); enabled = false; return; }
            if (DuckBenchmark.Running) { isolatedTest = true; Settings = Instantiate(Settings); Settings.totalDucks = DuckBenchmark.PopulationOverride; }
            if(DuckParkPlayChecks.Running)isolatedTest=true;
            var data = isolatedTest ? new SaveData { total = Settings.totalDucks, seed = Settings.seed } : DuckSaveSystem.Load(Settings);
            Progress = new Progression(Settings, data);
            Player=authoredPlayer; Stage=authoredStage;
            if(Player==null || Stage==null || Park==null) { Debug.LogError("Open the authored DuckPrototype scene; scene references are missing."); enabled=false; return; }
            Player.Initialize(this);
            if(data.hasPlayerPose) { Player.Teleport(Park.Land(data.playerPosition)+Vector3.up*.05f);Player.transform.rotation=Quaternion.Euler(0,data.playerYaw,0); }
            Population = new GameObject("Instanced duck population").AddComponent<DuckPopulationManager>(); Population.transform.SetParent(transform);
            Population.Initialize(Settings, data, Player.View);
            Physics=gameObject.AddComponent<DuckPhysics>(); Physics.Initialize(this);
            Park.casket.gameObject.SetActive(data.casketOwned); Park.casket.position=Park.Land(data.casketPosition);
            feedback = gameObject.AddComponent<DuckFeedback>(); feedback.Initialize(Population, Settings.animationPoolSize);
            HUD = gameObject.AddComponent<DuckHUD>(); HUD.Initialize(this);
            if(data.casketCarried && data.casketOwned) {
                CarryingCasket=true;Park.casket.SetParent(Player.View.transform,false);Park.casket.localPosition=new Vector3(0,-.65f,1.25f);Park.casket.localRotation=Quaternion.identity;
                foreach(var c in Park.casket.GetComponentsInChildren<Collider>())c.enabled=false;
            }
            boxScale = Stage.Box.localScale; RefreshTool();
            nextSave = Time.unscaledTime + Settings.autosaveSeconds;
            if (!isolatedTest) SetMenu(Progress.Complete);
            ShowNotice(Progress.Complete ? "EVERY DUCK, COLLECTED. Beautiful work." : "A sea of ducks. Start with one.", 5);
        }
        void Update()
        {
            if (Progress == null) return;
            SmoothedFrameMs = Mathf.Lerp(SmoothedFrameMs, Time.unscaledDeltaTime * 1000, .04f);
            if (Time.unscaledTime > noticeUntil) Notice = "";
            if (Progress.Dirty && Time.unscaledTime >= nextSave) Save();
            if (isolatedTest) return;
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { if (MenuOpen) SetMenu(false); else { Save(); SetMenu(true); } }
            if (keyboard.f3Key.wasPressedThisFrame) Diagnostics = !Diagnostics;
            if (MenuOpen) { Population.HoverId = -1; Prompt = ""; return; }
            if (Cursor.lockState != CursorLockMode.Locked) { if (mouse.leftButton.wasPressedThisFrame) SetMenu(false); return; }
            if (keyboard.digit1Key.wasPressedThisFrame) Equip(0);
            if (keyboard.digit2Key.wasPressedThisFrame) Equip(1);
            if (keyboard.digit3Key.wasPressedThisFrame) Equip(2);
            if (keyboard.digit4Key.wasPressedThisFrame) Equip(3);
            if (keyboard.digit5Key.wasPressedThisFrame) Equip(4);
            if (keyboard.fKey.wasPressedThisFrame)ToggleCasket();
            if(mouse.rightButton.isPressed && Time.time>=nextThrow) { nextThrow=Time.time+.18f; ThrowDuck(); }
            bool nearCasket=Progress.Data.casketOwned && (CarryingCasket || Near(Park.casket.position));
            bool nearBox = Near(Stage.BoxPosition), nearShop = Near(Stage.ShopPosition);
            var camera = Player.View.transform;
            int hovered = Population.Query(camera.position, camera.forward, Progress.Range, .4f, .28f);
            Population.HoverId = Progress.FreeSpace > 0 ? hovered : -1;
            Prompt = nearBox ? ((Progress.Data.carried > 0 || CarryingCasket && Progress.Data.casketDucks.Length>0) ? "E  /  DEPOSIT YOUR DUCKS" : "COLLECTION BOX  /  BRING BACK DUCKS") : nearShop ? "E  /  OPEN TOOL SHOP" :
                Progress.FreeSpace == 0 ? "FULL!  RETURN TO THE COLLECTION BOX" : hovered >= 0 ? ((Progress.Tool == DuckTool.Vacuum || Progress.Tool==DuckTool.IndustrialVacuum) ? "HOLD LMB  /  VACUUM" : Progress.Tool == DuckTool.Basket ? "HOLD LMB  /  SCOOP" : "HOLD LMB  /  PICK UP") : "LOOK AT A DUCK AND HOLD LMB";
            if(nearCasket && !nearBox && !nearShop)Prompt=CarryingCasket ? "F DROP CASKET / RETURN TO BOX / E STASH" : "E STASH DUCKS / F CARRY CASKET";
            if(Progress.Tool==DuckTool.Fork && !nearBox && !nearShop && !nearCasket)Prompt="HOLD LMB PUSH / RMB THROW / 1-5 TOOLS";
            if(CarryingCasket) Population.HoverId=-1;
            if (keyboard.eKey.wasPressedThisFrame)
            {
                if (nearBox) Deposit();
                else if(nearCasket && Progress.Data.carried>0) { ShowNotice("Stored "+Progress.Stash()+" ducks in casket."); Save(); }
                else if (nearShop) { ShopOpen = true; SetMenu(true, true); }
            }
            if (!MenuOpen && mouse.leftButton.isPressed && Time.time >= nextPickup && !CarryingCasket && (Progress.FreeSpace > 0 || Progress.Tool==DuckTool.Fork))
            {
                nextPickup = Time.time + Progress.Interval;
                if(Progress.Tool==DuckTool.Fork) { Physics.Push(Player.transform.position+Vector3.up*.2f,Player.transform.forward,3.3f,12,6); Stage.ToolModels[3].DOKill(); Stage.ToolModels[3].localRotation=Quaternion.identity; Stage.ToolModels[3].DOPunchRotation(new Vector3(18,0,0),.25f,2,.5f); }
                else Collect(hovered);
            }
        }
        bool Near(Vector3 point) { var delta = Player.transform.position - point; delta.y = 0; return delta.sqrMagnitude < 3.4f * 3.4f; }
        public int CollectAimed()
        {
            var camera = Player.View.transform;
            return Collect(Population.Query(camera.position, camera.forward, Progress.Range, .4f, .28f));
        }
        int Collect(int hovered)
        {
            var camera = Player.View.transform; int amount = 0;
            if(CarryingCasket || Progress.Tool==DuckTool.Fork)return 0;
            if (Progress.Tool == DuckTool.Hands)
            { if (CollectId(hovered)) amount = 1; }
            else
            {
                // Basket scoops a tight patch around the aimed duck; vacuum continuously searches its cone.
                Vector3 center = hovered >= 0 ? Population.Position(hovered) : Vector3.zero;
                if (Progress.Tool == DuckTool.Basket && hovered < 0) return 0;
                for (int i = 0; i < Progress.Equipment.batch && Progress.FreeSpace > 0; i++)
                {
                    int id = (Progress.Tool == DuckTool.Vacuum || Progress.Tool==DuckTool.IndustrialVacuum) ? Population.Query(camera.position, camera.forward, Progress.Range, .72f) :
                        Population.Query(center + Vector3.up * 1.2f, Vector3.down, 2.8f, .1f);
                    if (!CollectId(id)) break; amount++;
                }
            }
            if (amount == 0) return 0;
            if (Time.time > burstUntil) pickupBurst = 0;
            pickupBurst += amount; burstUntil = Time.time + .7f;
            ShowNotice("+" + pickupBurst + " ducks", 1);
            feedback.PickupSound((float)Progress.Data.carried / Progress.Capacity);
            return amount;
        }
        public bool CollectId(int id)
        {
            if ((!Population.IsAvailable(id) && !Population.IsPhysical(id)) || Progress.FreeSpace <= 0 || CarryingCasket) return false;
            if (!Progress.PickUp()) return false;
            Progress.RecordPickup(id); Physics.Release(id); Population.Remove(id); feedback.Fly(Population.Position(id), Population.Angle(id), Player.CarryTarget); return true;
        }
        public int Deposit()
        {
            if(!Near(Stage.BoxPosition)) { ShowNotice("Bring your ducks to the collection box."); return 0; }
            int crateAmount=CarryingCasket ? Progress.Data.casketDucks.Length : 0;
            if(crateAmount>0) { Progress.Data.deposited+=crateAmount; Progress.Data.money+=crateAmount*Settings.moneyPerDuck; Progress.Data.casketDucks=System.Array.Empty<int>(); Progress.Touch(); }
            int amount = Progress.Deposit()+crateAmount; if (amount == 0) { ShowNotice("Pick up some ducks first."); return 0; }
            for (int i = 0; i < Mathf.Min(12, amount); i++) feedback.Fly(Player.CarryTarget.position + Random.insideUnitSphere * .18f, i * 41, Stage.DepositTarget, true);
            boxTween?.Kill(); Stage.Box.localScale = boxScale;
            boxTween = Stage.Box.DOPunchScale(Vector3.one * .10f, .45f, 4, .5f);
            feedback.DepositSound(amount); ShowNotice("+" + amount + " DUCKS     +$" + amount * Settings.moneyPerDuck, 3);
            Save(); if (Progress.Complete) { ShowNotice("EVERY DUCK, COLLECTED. Beautiful work.", 60); SetMenu(true); }
            return amount;
        }
        public void BuyTool(int index)
        { if (Progress.BuyTool(index)) { RefreshTool(); Save(); ShowNotice(Settings.tools[index].name + " ready!"); } }
        public void BuyUpgrade(int index)
        { if (Progress.BuyUpgrade(index)) { Save(); ShowNotice("Upgraded. Go collect more ducks!"); } }
        public void Equip(int index)
        {
            if (Progress.Equip(index)) { RefreshTool(); Save(); }
            else if (index < Progress.Data.owned.Length && Progress.Data.owned[index]) ShowNotice("Deposit first: that tool has a smaller capacity.");
        }
        void RefreshTool() { for (int i = 0; i < Stage.ToolModels.Length; i++) Stage.ToolModels[i].gameObject.SetActive(i == Progress.Data.currentTool && !CarryingCasket); }
        public void SetMenu(bool open, bool shop = false)
        {
            MenuOpen = open; ShopOpen = open && shop;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = open;
            if (HUD != null) HUD.Refresh();
        }
        public void ShowNotice(string text, float duration = 2) { Notice = text; noticeUntil = Time.unscaledTime + duration; }
        public bool Save()
        {
            if (Progress == null || Population == null || Park == null || Player == null || isolatedTest) return false;
            Progress.Data.collected = Population.CollectedIds();
            Progress.Data.poses=Population.Poses();
            Progress.Data.casketPosition=Park.Land(Park.casket.position);
            Progress.Data.casketCarried=CarryingCasket;Progress.Data.hasPlayerPose=true;Progress.Data.playerPosition=Player.transform.position;Progress.Data.playerYaw=Player.transform.eulerAngles.y;
            bool ok = DuckSaveSystem.Save(Progress.Data);
            if (ok) Progress.MarkSaved(); else ShowNotice("SAVE FAILED — " + DuckSaveSystem.LastError, 15);
            nextSave = Time.unscaledTime + Settings.autosaveSeconds; return ok;
        }
        public void StartFresh()
        {
            if (isolatedTest) return;
            var fresh = new SaveData { total = Settings.totalDucks, seed = Settings.seed };
            if (!DuckSaveSystem.Save(fresh)) { ShowNotice("Could not start a new game: " + DuckSaveSystem.LastError, 10); return; }
            // Suppress the old scene's exit save after committing the new run.
            isolatedTest = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationFocus(bool focus) { if (!focus && Progress != null && !isolatedTest) { Save(); SetMenu(true); } }
        void OnApplicationQuit() => Save();
        void OnDisable() { if (Progress != null && Population != null) Save(); }
        void OnDestroy() { boxTween?.Kill(); if (DuckBenchmark.Running && Settings != null) Destroy(Settings); if (!isolatedTest) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; } }
    }
}
