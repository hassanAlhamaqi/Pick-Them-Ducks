using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sandouq.Ducks
{
    [DefaultExecutionOrder(-100)]
    public sealed class DuckGame : MonoBehaviour
    {
        public PrototypeSettings Settings;
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
            var data = isolatedTest ? new SaveData { total = Settings.totalDucks, seed = Settings.seed } : DuckSaveSystem.Load(Settings);
            Progress = new Progression(Settings, data);
            var playerGo = new GameObject("Player", typeof(CharacterController), typeof(DuckPlayer)); playerGo.transform.SetParent(transform);
            Player = playerGo.GetComponent<DuckPlayer>(); Player.Initialize(this); Player.Teleport(new Vector3(0, .05f, -2));
            Population = new GameObject("Instanced duck population").AddComponent<DuckPopulationManager>(); Population.transform.SetParent(transform);
            Population.Initialize(Settings, data, Player.View);
            Stage = new GameObject("Single stage").AddComponent<DuckStage>(); Stage.transform.SetParent(transform); Stage.Build(Population.Width, Population.Length, Player.CarryTarget);
            feedback = gameObject.AddComponent<DuckFeedback>(); feedback.Initialize(Population, Settings.animationPoolSize);
            HUD = gameObject.AddComponent<DuckHUD>(); HUD.Initialize(this);
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
            bool nearBox = Near(Stage.BoxPosition), nearShop = Near(Stage.ShopPosition);
            var camera = Player.View.transform;
            int hovered = Population.Query(camera.position, camera.forward, Progress.Range, .4f, .28f);
            Population.HoverId = Progress.FreeSpace > 0 ? hovered : -1;
            Prompt = nearBox ? (Progress.Data.carried > 0 ? "E  /  DEPOSIT YOUR DUCKS" : "COLLECTION BOX  /  BRING BACK DUCKS") : nearShop ? "E  /  OPEN TOOL SHOP" :
                Progress.FreeSpace == 0 ? "FULL!  RETURN TO THE COLLECTION BOX" : hovered >= 0 ? (Progress.Tool == DuckTool.Vacuum ? "HOLD LMB  /  VACUUM" : Progress.Tool == DuckTool.Basket ? "HOLD LMB  /  SCOOP" : "HOLD LMB  /  PICK UP") : "LOOK AT A DUCK AND HOLD LMB";
            if (keyboard.eKey.wasPressedThisFrame)
            {
                if (nearBox) Deposit();
                else if (nearShop) { ShopOpen = true; SetMenu(true, true); }
            }
            if (!MenuOpen && mouse.leftButton.isPressed && Time.time >= nextPickup && Progress.FreeSpace > 0)
            {
                nextPickup = Time.time + Progress.Interval;
                Collect(hovered);
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
            if (Progress.Tool == DuckTool.Hands)
            { if (CollectId(hovered)) amount = 1; }
            else
            {
                // Basket scoops a tight patch around the aimed duck; vacuum continuously searches its cone.
                Vector3 center = hovered >= 0 ? Population.Position(hovered) : Vector3.zero;
                if (Progress.Tool == DuckTool.Basket && hovered < 0) return 0;
                for (int i = 0; i < Progress.Equipment.batch && Progress.FreeSpace > 0; i++)
                {
                    int id = Progress.Tool == DuckTool.Vacuum ? Population.Query(camera.position, camera.forward, Progress.Range, .72f) :
                        Population.Query(center + Vector3.up * 1.2f, Vector3.down, 1.7f, .1f);
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
            if (!Population.IsAvailable(id) || Progress.FreeSpace <= 0) return false;
            if (!Progress.PickUp()) return false;
            Population.Remove(id); feedback.Fly(Population.Position(id), Population.Angle(id), Player.CarryTarget); return true;
        }
        public int Deposit()
        {
            int amount = Progress.Deposit(); if (amount == 0) { ShowNotice("Pick up some ducks first."); return 0; }
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
        void RefreshTool() { for (int i = 0; i < Stage.ToolModels.Length; i++) Stage.ToolModels[i].gameObject.SetActive(i == Progress.Data.currentTool); }
        public void SetMenu(bool open, bool shop = false)
        {
            MenuOpen = open; ShopOpen = open && shop;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = open;
            if (HUD != null) HUD.Refresh();
        }
        public void ShowNotice(string text, float duration = 2) { Notice = text; noticeUntil = Time.unscaledTime + duration; }
        public bool Save()
        {
            if (Progress == null || isolatedTest) return false;
            Progress.Data.collected = Population.CollectedIds();
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
