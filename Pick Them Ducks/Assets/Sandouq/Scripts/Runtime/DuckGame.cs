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
        public bool isolatedTest;
        public DuckHUD hudPrefab;
        public DuckHUD authoredHUD;
        public Progression Progress {get;private set;}
        public DuckPopulationManager Population {get;private set;}
        public DuckPhysics Physics {get;private set;}
        public DuckDeposits Deposits {get;private set;}
        public DuckPlayer Player {get;private set;}
        public DuckStage Stage {get;private set;}
        public DuckHUD HUD {get;private set;}
        public bool MenuOpen {get;private set;}
        public bool ShopOpen {get;private set;}
        public bool Diagnostics {get;private set;}
        public string Prompt {get;private set;}
        public string Notice {get;private set;}
        public float SmoothedFrameMs {get;private set;}
        public float PickupCooldownProgress => Time.time < nextHandPickup ? Mathf.Clamp01((nextHandPickup-Time.time)/handCooldownDuration) : 0;
        public bool ToolActive {get;private set;}
        public bool Placing {get;private set;}
        public bool Throwing => throwHeld>0;
        public bool CanDrive=>ToolActive&&!Deposits.Transferring&&Progress.FreeSpace>0&&!Player.Braking&&(Progress.Tool==DuckTool.Collector||Progress.Tool==DuckTool.RollerCar);
        DuckFeedback feedback;
        DuckHabitat[] habitats;DuckHabitat hoveredHabitat;
        GameObject placementPreview;
        Vector3 previousToolPosition;
        float nextSave,noticeUntil,nextThrow,throwHeld,nextCollect,nextHandPickup,handCooldownDuration,placementYaw;
        readonly Collider[] placementHits=new Collider[64];
        MaterialPropertyBlock previewColor;
        void Start()
        {
            previewColor=new MaterialPropertyBlock();
            if(Settings==null||Settings.duckPrefab==null){enabled=false;return;}
            if(DuckBenchmark.Running){isolatedTest=true;Settings=Instantiate(Settings);Settings.totalDucks=DuckBenchmark.PopulationOverride;}
            if(DuckParkPlayChecks.Running)isolatedTest=true;
            var data=isolatedTest?new SaveData{total=Settings.totalDucks,seed=Settings.seed}:DuckSaveSystem.Load(Settings);
            Progress=new Progression(Settings,data);Player=authoredPlayer;Stage=authoredStage;
            if(Player==null||Stage==null||Park==null||Park.casketPrefab==null){Debug.LogError("Open the updated authored DuckPrototype scene.");enabled=false;return;}
            Player.Initialize(this);
            if(data.hasPlayerPose){var restored=data.playerPosition;if(Park.WalkableWater(restored))restored.y=Park.WaterSupportHeight(restored)+.05f;else restored=Park.Land(restored)+Vector3.up*.05f;Player.Teleport(restored);Player.transform.rotation=Quaternion.Euler(0,data.playerYaw,0);}
            Population=new GameObject("Instanced duck population").AddComponent<DuckPopulationManager>();Population.transform.SetParent(transform);Population.Initialize(Settings,data,Player.View);
            Physics=gameObject.AddComponent<DuckPhysics>();Physics.Initialize(this);
            feedback=gameObject.AddComponent<DuckFeedback>();feedback.Initialize(Population,Settings.animationPoolSize);
            if(Park.casket!=null)Park.casket.gameObject.SetActive(false);
            Deposits=gameObject.AddComponent<DuckDeposits>();Deposits.Initialize(this);
            habitats=Park.GetComponentsInChildren<DuckHabitat>();var moved=new System.Collections.Generic.HashSet<int>();if(data.poses!=null)foreach(var pose in data.poses)moved.Add(pose.id);foreach(var habitat in habitats)habitat.Initialize(this,moved);
            HUD=authoredHUD!=null?authoredHUD:hudPrefab!=null?Instantiate(hudPrefab,transform):gameObject.AddComponent<DuckHUD>();HUD.Initialize(this);
            RefreshTool();previousToolPosition=Player.transform.position;nextSave=Time.unscaledTime+Settings.autosaveSeconds;
            if(!isolatedTest)SetMenu(Progress.Complete);
        }
        void Update()
        {
            if(Deposits==null)return;
            SmoothedFrameMs=Mathf.Lerp(SmoothedFrameMs,Time.unscaledDeltaTime*1000,.04f);
            if(Time.unscaledTime>noticeUntil)Notice="";
            if(!isolatedTest&&Progress.Dirty&&Time.unscaledTime>=nextSave)Save();
            if(isolatedTest)return;
            var keys=Keyboard.current;var mouse=Mouse.current;if(keys==null||mouse==null)return;
            if(keys.escapeKey.wasPressedThisFrame){if(Placing)CancelPlacement();else SetMenu(!MenuOpen);}
            if(keys.tabKey.wasPressedThisFrame)SetMenu(!MenuOpen,true);
            if(keys.f3Key.wasPressedThisFrame)Diagnostics=!Diagnostics;
            if(hoveredHabitat!=null){hoveredHabitat.Hovered=false;hoveredHabitat=null;}
            if(MenuOpen){ToolActive=false;Population.HoverId=-1;return;}
            if(Cursor.lockState!=CursorLockMode.Locked){if(mouse.leftButton.wasPressedThisFrame)SetMenu(false);return;}
            if(keys.fKey.wasPressedThisFrame){if(Placing)CancelPlacement();else BeginPlacement();}
            if(Placing){UpdatePlacement(mouse,keys);return;}
            if(keys.digit1Key.wasPressedThisFrame)Equip(0);
            if(keys.digit2Key.wasPressedThisFrame)Equip(3);
            if(keys.digit3Key.wasPressedThisFrame)Equip(1);
            if(keys.digit4Key.wasPressedThisFrame)Equip(2);
            if(keys.digit5Key.wasPressedThisFrame)Equip(4);
            var station=Deposits.Nearest(Player.transform.position);
            bool shop=Near(Stage.ShopPosition);
            var habitat=FocusedHabitat();
            if(keys.eKey.wasPressedThisFrame){if(station!=null)Deposit();else if(shop)SetMenu(true,true);else if(habitat!=null)habitat.Interact();}
            ToolActive=mouse.leftButton.isPressed&&!Deposits.Transferring;
            var camera=Player.View.transform;
            int id=Population.Query(camera.position,camera.forward,Progress.Range,.4f,.3f);
            bool habitatInput=habitat!=null&&id<0;
            if(habitatInput){hoveredHabitat=habitat;habitat.Hovered=true;if(mouse.leftButton.wasPressedThisFrame)habitat.Interact();ToolActive=false;}
            Population.HoverId=Progress.FreeSpace>0&&(Progress.Tool==DuckTool.Hands||Progress.Tool==DuckTool.Vacuum)?id:-1;
            Prompt=Deposits.Transferring?"DEPOSITING DUCKS / E STOP / WALK AWAY TO CANCEL":station!=null?"E DEPOSIT / PUSH DUCKS INTO THE INTAKE":shop?"E OPEN TOOL SHOP":habitat!=null?habitat.Hint:Progress.FreeSpace==0&&(Progress.Tool==DuckTool.Hands||Progress.Tool==DuckTool.Vacuum)?"BAG FULL / FIND A DEPOSIT STATION":Progress.Tool==DuckTool.Hands?"CLICK LMB ON A DUCK TO PICK UP":Progress.Tool==DuckTool.Sweeper?"HOLD LMB + WALK TO SWEEP DUCKS":(Progress.Tool==DuckTool.Collector||Progress.Tool==DuckTool.RollerCar)?"HOLD LMB TO COLLECT / RELEASE TO PULL DUCKS INTO YOUR BAG":"HOLD LMB TO USE / RELEASE TO STOP";
            if(Progress.Tool==DuckTool.Hands)TickHand(id,ToolActive&&mouse.leftButton.wasPressedThisFrame);
            else {if(ToolActive&&Progress.Tool==DuckTool.Vacuum&&Time.time>=nextCollect){nextCollect=Time.time+Progress.Interval;CollectAimed();}}
            TickThrow(mouse.rightButton.isPressed,Time.deltaTime);
        }
        public bool TickHand(int id,bool clicked)
        {
            if(!clicked||id<0||Time.time<nextHandPickup||MenuOpen||Placing||Progress.FreeSpace<=0||Deposits.Transferring)return false;
            Vector3 center=Population.Position(id);int amount=0;
            for(int i=0;i<Progress.PickupAmount;i++){int candidate=i==0?id:Population.Query(center+Vector3.up*1.2f,Vector3.down,2.2f,.1f);if(!CollectId(candidate))break;amount++;}
            if(amount==0)return false;
            handCooldownDuration=Mathf.Max(.01f,Progress.Interval);nextHandPickup=Time.time+handCooldownDuration;return true;
        }
        void FixedUpdate()
        {
            if(Deposits==null)return;
            var movement=Player.transform.position-previousToolPosition;previousToolPosition=Player.transform.position;
            if(MenuOpen||!ToolActive||Placing||Deposits.Transferring||(Progress.Tool==DuckTool.Sweeper&&movement.sqrMagnitude<.0001f))return;
            SweepFloor(movement);
        }
        public int SweepFloor(Vector3 movement)
        {
            var tool=Progress.Tool;if(tool!=DuckTool.Sweeper&&tool!=DuckTool.Collector&&tool!=DuckTool.RollerCar)return 0;
            float reach=tool==DuckTool.RollerCar?2.2f:1.5f;
            var localMovement=Player.transform.InverseTransformDirection(movement);
            var box=new Bounds(new Vector3(-localMovement.x*.5f,.4f,reach-localMovement.z*.5f),new Vector3(Progress.WorkingWidth+Mathf.Abs(localMovement.x),1.5f,1+Mathf.Min(Mathf.Abs(localMovement.z),2)));
            if(tool==DuckTool.Sweeper)
            {
                Vector3 velocity=movement.normalized*Mathf.Clamp(movement.magnitude/Time.fixedDeltaTime+1,2,14);
                Physics.SweepActive(Player.transform,box,velocity);int pushed=0;
                for(int i=0;i<24;i++){int id=Population.QueryBox(Player.transform,box,false);if(id<0||!Physics.Launch(id,Population.Position(id),velocity+Vector3.up*.15f))break;pushed++;}return pushed;
            }
            if(Time.time<nextCollect)return 0;nextCollect=Time.time+Progress.Interval;return Physics.CorralRoller(box);
        }
        public int CollectAimed()
        {
            if(Progress.Tool!=DuckTool.Vacuum||Deposits.Transferring)return 0;
            var camera=Player.View.transform;int amount=0;
            for(int i=0;i<Progress.PickupAmount;i++){int id=Population.Query(camera.position,camera.forward,Progress.Range,.72f);if(!CollectId(id))break;amount++;}return amount;
        }
        public bool RecordRollerPickup(int id)
        {
            if((!Population.IsAvailable(id)&&!Population.IsPhysical(id))||!Progress.PickUp())return false;
            Progress.RecordPickup(id);Population.Remove(id);Physics.CollapsePile(Population.Position(id));Progress.Touch();return true;
        }
        public bool CollectId(int id)
        {
            if(Deposits.Transferring||Placing||(!Population.IsAvailable(id)&&!Population.IsPhysical(id))||!Progress.PickUp())return false;
            Progress.RecordPickup(id);Physics.Release(id);Population.Remove(id);Physics.CollapsePile(Population.Position(id));feedback.Fly(Population.Position(id),Population.Angle(id),Player.CarryTarget);feedback.PickupSound((float)Progress.Data.carried/Progress.Capacity);return true;
        }
        public void TickThrow(bool held,float deltaTime)
        {
            if(!held||MenuOpen||Placing||Deposits.Transferring||Progress.Data.carried==0){throwHeld=0;nextThrow=0;return;}
            throwHeld+=deltaTime;
            if(Time.time>=nextThrow){nextThrow=Time.time+Mathf.Lerp(.18f,.035f,Mathf.Clamp01(throwHeld/3));ThrowDuck();}
        }
        public bool ThrowDuck()
        {
            Physics.FlushFront();
            int id=Progress.ThrowId;if(id<0||Placing||Deposits.Transferring)return false;var camera=Player.View.transform;
            Physics.Release(id);
            if(!Physics.Launch(id,camera.position+camera.forward*.7f,camera.forward*8+Vector3.up*2,true))return false;Progress.Thrown();return true;
        }
        public int Deposit(){Physics.FlushFront();ToolActive=false;bool stopping=Deposits.Transferring;int amount=Deposits.Begin();ShowNotice(stopping?"Stopping after ducks in flight land.":amount>0?"Depositing one duck at a time...":"Bring ducks to a deposit station.");return amount;}
        public void DepositLanded(){feedback.DepositSound(1);if(Progress.Complete){Save();SetMenu(true);ShowNotice("EVERY DUCK DEPOSITED!",60);}}
        public DuckHabitat FocusedHabitat()
        {
            DuckHabitat result=null;float nearest=float.PositiveInfinity;
            if(habitats==null)return null;
            var ray=new Ray(Player.View.transform.position,Player.View.transform.forward);
            foreach(var h in habitats)if(h.Available&&h.RayHit(ray,out float distance)&&distance<nearest){result=h;nearest=distance;}
            return result;
        }
        bool Near(Vector3 p)=>(Player.transform.position-p).sqrMagnitude<3.4f*3.4f;
        public void BuyCasket(){if(Progress.BuyCasket()){Save();ShowNotice("Duck Casket kit purchased. F to install.");}}
        public void BeginPlacement()
        {
            if(Progress.Data.casketKits==0||Deposits.Transferring){ShowNotice("Buy a Duck Casket kit at the shop first.");return;}
            Placing=true;throwHeld=0;nextThrow=0;ToolActive=false;placementYaw=Player.transform.eulerAngles.y;
            placementPreview=Instantiate(Park.casketPrefab);placementPreview.name="Duck Casket placement preview";placementPreview.SetActive(true);
            foreach(var c in placementPreview.GetComponentsInChildren<Collider>())c.enabled=false;
            placementPreview.GetComponent<DuckDepositStation>().enabled=false;
        }
        public bool CanInstall(Vector3 p,float yaw)
        {
            if(Park.InLake(p)||Mathf.Abs(p.x)>142||p.z< -15||p.z>277)return false;
            int n=UnityEngine.Physics.OverlapBoxNonAlloc(p+Vector3.up*.65f,new Vector3(1.3f,.55f,1.2f),placementHits,Quaternion.Euler(0,yaw,0),~0,QueryTriggerInteraction.Ignore);
            if(n==placementHits.Length)return false;
            for(int i=0;i<n;i++)if(!(placementHits[i] is TerrainCollider)&&placementHits[i].attachedRigidbody==null)return false;
            foreach(var station in Deposits.Stations)if((station.transform.position-p).sqrMagnitude<9)return false;
            float h=Park.Ground(p);return Mathf.Abs(Park.Ground(p+Vector3.right)-h)<.35f&&Mathf.Abs(Park.Ground(p+Vector3.forward)-h)<.35f;
        }
        public bool InstallCasket(Vector3 p,float yaw)
        {
            if(!CanInstall(p,yaw)||!Progress.InstallCasket(p,yaw))return false;
            Deposits.AddStation(new CasketPlacement{position=p,yaw=yaw});CancelPlacement();Save();ShowNotice("Duck Casket installed. Push ducks in or press E to deposit.");return true;
        }
        void UpdatePlacement(Mouse mouse,Keyboard keys)
        {
            if(keys.rKey.wasPressedThisFrame)placementYaw+=45;
            var p=Park.Land(Player.transform.position+Player.transform.forward*3.5f);bool valid=CanInstall(p,placementYaw);
            PreviewPlacement(p,placementYaw);
            Prompt=valid?"LMB INSTALL DUCK CASKET / R ROTATE / RMB CANCEL":"BLOCKED OR TOO STEEP / MOVE TO CLEAR GROUND / RMB CANCEL";
            if(mouse.rightButton.wasPressedThisFrame)CancelPlacement();else if(mouse.leftButton.wasPressedThisFrame&&valid)InstallCasket(p,placementYaw);
        }
        public void PreviewPlacement(Vector3 p,float yaw)
        {
            if(!Placing)return;bool valid=CanInstall(p,yaw);
            placementPreview.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));previewColor.SetColor("_BaseColor",valid?new Color(.2f,.9f,.5f):new Color(1,.2f,.15f));
            foreach(var renderer in placementPreview.GetComponentsInChildren<Renderer>())renderer.SetPropertyBlock(previewColor);
        }
        public void CancelPlacement(){Placing=false;if(placementPreview!=null)Destroy(placementPreview);}
        public void BuyTool(int i){if(Progress.BuyTool(i)){RefreshTool();Save();}}
        public void BuyUpgrade(int i){if(Progress.BuyUpgrade(i)){RefreshTool();Save();}}
        public void BuyToolUpgrade()=>BuyToolUpgrade(Progress.Data.currentTool);
        public void BuyToolUpgrade(int index){if(Progress.BuyToolUpgrade(index)){RefreshTool();Save();}}
        public void Equip(int i){if(Deposits.Transferring)return;if(Progress.Equip(i)){Physics.FlushFront();ToolActive=false;RefreshTool();Save();}else ShowNotice("Buy this tool at the shop first.");}
        void RefreshTool()
        {
            for(int i=0;i<Stage.ToolModels.Length;i++)
            {
                var model=Stage.ToolModels[i];model.gameObject.SetActive(i==Progress.Data.currentTool);
                if(i==1||i==3||i==4)model.localScale=new Vector3(1+Progress.Data.toolLevels[i]*.18f,1,1);
            }
            Player.View.transform.localPosition=Vector3.up*(Progress.Tool==DuckTool.RollerCar?2.15f:1.7f);
        }
        public void SetMenu(bool open,bool shop=false){if(Physics!=null)Physics.FlushFront();throwHeld=0;nextThrow=0;MenuOpen=open;ShopOpen=open&&shop;ToolActive=false;Cursor.lockState=open?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=open;if(HUD!=null)HUD.Refresh();if(open)Save();}
        public void ShowNotice(string text,float duration=2){Notice=text;noticeUntil=Time.unscaledTime+duration;}
        public bool Save()
        {
            if(Progress==null||Population==null||Player==null||isolatedTest)return false;
            Progress.Data.collected=Population.CollectedIds();Progress.Data.poses=Population.Poses();Progress.Data.hasPlayerPose=true;Progress.Data.playerPosition=Player.transform.position;Progress.Data.playerYaw=Player.transform.eulerAngles.y;
            bool ok=DuckSaveSystem.Save(Progress.Data);if(ok)Progress.MarkSaved();else ShowNotice("SAVE FAILED: "+DuckSaveSystem.LastError,15);nextSave=Time.unscaledTime+Settings.autosaveSeconds;return ok;
        }
        public void StartFresh(){if(isolatedTest)return;if(!DuckSaveSystem.Save(new SaveData{total=Settings.totalDucks,seed=Settings.seed}))return;isolatedTest=true;UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);}
        void OnApplicationPause(bool paused){if(paused)Save();}
        void OnApplicationFocus(bool focused){if(!focused&&!isolatedTest&&Deposits!=null)SetMenu(true);}
        void OnApplicationQuit()=>Save();
        void OnDisable()=>Save();
        void OnDestroy(){if(!isolatedTest){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}if(DuckBenchmark.Running&&Settings!=null)Destroy(Settings);}
    }
}
