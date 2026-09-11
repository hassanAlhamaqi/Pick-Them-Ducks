using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sandouq.Ducks.Editor
{
    public static class DuckToolsBuilder
    {
        const string Folder="Assets/Sandouq/Park/Prefabs/";
        static Material metal, yellow, rubber, red;
        public static void ConfigureSettings(PrototypeSettings settings)
        {
            var defaults=ScriptableObject.CreateInstance<PrototypeSettings>();
            settings.tools=defaults.tools;settings.upgrades=defaults.upgrades;settings.casketCost=450;settings.depositInterval=.09f;settings.depositFlightTime=.32f;
            Object.DestroyImmediate(defaults);EditorUtility.SetDirty(settings);
        }
        public static void UpgradeScene()
        {
            EditorSceneManager.OpenScene(DuckPrototypeBuilder.ScenePath);
            var game=Object.FindAnyObjectByType<DuckGame>();ConfigureSettings(game.Settings);
            metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Sandouq/Park/Materials/Deep teal.mat");
            yellow=AssetDatabase.LoadAssetAtPath<Material>("Assets/Sandouq/Park/Materials/Marigold.mat");
            rubber=AssetDatabase.LoadAssetAtPath<Material>("Assets/Sandouq/Park/Materials/Box interior.mat");
            red=AssetDatabase.LoadAssetAtPath<Material>("Assets/Sandouq/Park/Materials/Coral.mat");
            var stage=game.authoredStage;var player=game.authoredPlayer;
            // Unpack the player instance before replacing only its tool children; keep the authored terrain and landmarks.
            if(PrefabUtility.IsPartOfPrefabInstance(player))PrefabUtility.UnpackPrefabInstance(player.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            foreach(var tool in stage.ToolModels)if(tool!=null)Object.DestroyImmediate(tool.gameObject);
            stage.ToolModels=new Transform[5];
            var hands=new GameObject("Hands");Part(hands,"Glove",new Vector3(.06f,-.06f,-.06f),new Vector3(.18f,.11f,.28f),red);
            stage.ToolModels[0]=SaveTool(hands,"Hands Tool",player.CarryTarget,new Vector3(0,0,0),Vector3.one*.65f);
            var sweeper=new GameObject("Duck Sweeper");Part(sweeper,"Brush back",new Vector3(0,.22f,1.5f),new Vector3(2.4f,.22f,.36f),yellow);
            for(int i=0;i<17;i++)Part(sweeper,"Bristles",new Vector3((i-8)*.14f,.1f,1.5f),new Vector3(.095f,.2f,.28f),rubber);
            var handle=Part(sweeper,"Long handle",new Vector3(0,.75f,.75f),new Vector3(.07f,.08f,1.8f),metal);handle.transform.localRotation=Quaternion.Euler(45,0,0);
            stage.ToolModels[3]=SaveTool(sweeper,"Duck Sweeper",player.transform,Vector3.zero,Vector3.one);
            var collector=new GameObject("Duck Collector");Part(collector,"Collector tray",new Vector3(0,.3f,1.3f),new Vector3(1.4f,.4f,.9f),red);
            Roller(collector,"Roller drum",new Vector3(0,.3f,1.8f),1.4f,.3f,yellow);
            for(int i=-1;i<=1;i+=2){Roller(collector,"Wheel",new Vector3(i*.78f,.28f,1.05f),.15f,.28f,rubber);var arm=Part(collector,"Handle arm",new Vector3(i*.4f,.75f,.6f),new Vector3(.06f,.06f,1.5f),metal);arm.transform.localRotation=Quaternion.Euler(40,0,0);}
            stage.ToolModels[1]=SaveTool(collector,"Duck Collector",player.transform,Vector3.zero,Vector3.one);
            var vacuum=new GameObject("Duck Vacuum");Part(vacuum,"Canister",Vector3.zero,new Vector3(.3f,.4f,.5f),red);Part(vacuum,"Tube",new Vector3(0,0,.5f),new Vector3(.15f,.15f,.7f),metal);Part(vacuum,"Intake",new Vector3(0,0,.85f),new Vector3(.4f,.2f,.15f),yellow);
            stage.ToolModels[2]=SaveTool(vacuum,"Duck Vacuum",player.CarryTarget,Vector3.zero,Vector3.one*.8f);
            var car=new GameObject("Duck Roller Car");Part(car,"Chassis",new Vector3(0,.65f,.45f),new Vector3(2.4f,.4f,3.7f),metal);
            Part(car,"Hood",new Vector3(0,1.05f,1.1f),new Vector3(2.25f,.5f,1.2f),yellow);Part(car,"Seat",new Vector3(0,1.12f,-.45f),new Vector3(.85f,.5f,.8f),rubber);
            Part(car,"Seat back",new Vector3(0,1.65f,-.85f),new Vector3(.85f,.95f,.18f),rubber);Part(car,"Steering console",new Vector3(0,1.45f,.25f),new Vector3(.8f,.16f,.3f),red);
            Roller(car,"Roller drum",new Vector3(0,.45f,2.2f),3.5f,.45f,yellow);
            for(int side=-1;side<=1;side+=2)for(int wheel=-1;wheel<=1;wheel+=2)Roller(car,"Wheel",new Vector3(side*1.2f,.45f,wheel*1.1f),.35f,.45f,rubber);
            stage.ToolModels[4]=SaveTool(car,"Duck Roller Car",player.transform,Vector3.zero,Vector3.one);
            var casket=new GameObject("Duck Casket");Part(casket,"Floor",new Vector3(0,.06f,0),new Vector3(2.1f,.12f,1.9f),metal,true);
            Part(casket,"Back wall",new Vector3(0,.5f,.9f),new Vector3(2.1f,1,.12f),yellow,true);
            for(int i=-1;i<=1;i+=2)Part(casket,"Side wall",new Vector3(i*1f,.5f,0),new Vector3(.12f,1,1.9f),yellow,true);
            Part(casket,"Intake lip",new Vector3(0,.08f,-1f),new Vector3(2.2f,.16f,.4f),red,false);
            var receiver=casket.AddComponent<DuckDepositStation>();receiver.bounceRoot=casket.transform;
            receiver.landing=new GameObject("Duck landing").transform;receiver.landing.SetParent(casket.transform,false);receiver.landing.localPosition=new Vector3(0,.3f,.3f);
            game.Park.casketPrefab=PrefabUtility.SaveAsPrefabAsset(casket,Folder+"Duck Casket.prefab");Object.DestroyImmediate(casket);
            if(game.Park.casket!=null)game.Park.casket.gameObject.SetActive(false);
            var depot=stage.Box.GetComponent<DuckDepositStation>();if(depot==null)depot=stage.Box.gameObject.AddComponent<DuckDepositStation>();depot.landing=stage.DepositTarget;depot.bounceRoot=stage.Box;
            // The front intake catches pushed ducks before the authored box's rim collider.
            depot.intake=new Bounds(new Vector3(0,.4f,-.3f),new Vector3(2.2f,1.5f,2.8f));
            foreach(var tool in stage.ToolModels)tool.gameObject.SetActive(tool==stage.ToolModels[0]);
            PrefabUtility.SaveAsPrefabAssetAndConnect(player.gameObject,Folder+"Player.prefab",InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);
            AssetDatabase.SaveAssets();
        }
        static Transform SaveTool(GameObject root,string name,Transform parent,Vector3 position,Vector3 scale)
        {
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+name+".prefab");Object.DestroyImmediate(root);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);instance.transform.localPosition=position;instance.transform.localScale=scale;return instance.transform;
        }
        static GameObject Part(GameObject root,string name,Vector3 position,Vector3 scale,Material material,bool collider=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root.transform,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static void Roller(GameObject root,string name,Vector3 position,float width,float radius,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(root.transform,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,0,90);go.transform.localScale=new Vector3(radius*2,width*.5f,radius*2);go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        public static void UpgradeAndBuild(){UpgradeScene();DuckPrototypeChecks.Run(AssetDatabase.LoadAssetAtPath<PrototypeSettings>(DuckPrototypeBuilder.SettingsPath));DuckPrototypeBuilder.BuildValidationPlayer();}
    }
}
