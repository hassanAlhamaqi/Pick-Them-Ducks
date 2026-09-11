using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sandouq.Ducks.Editor
{
    public static class DuckParkChecks
    {
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        public static void Run()
        {
            Directory.CreateDirectory("Logs");Directory.CreateDirectory("Temp/DuckValidation");
            EditorSceneManager.OpenScene(DuckPrototypeBuilder.ScenePath);
            var game=UnityEngine.Object.FindAnyObjectByType<DuckGame>();
            Check(game!=null && game.authoredPlayer!=null && game.authoredStage!=null && game.Park!=null,"Serialized scene references");
            Check(PrefabUtility.IsPartOfPrefabInstance(game.authoredStage.Shop),"Shop must be a prefab instance");
            Check(PrefabUtility.IsPartOfPrefabInstance(game.Park.casket),"Casket must be a prefab instance");
            Check(game.Park.terrain.terrainData.size.x>=300,"Expanded terrain");
            DuckPrototypeChecks.Run(game.Settings);
            var data=new SaveData{total=2000};var progress=new Progression(game.Settings,data);data.money=450;Check(progress.BuyCasket(),"Casket purchase");
            for(int trip=0;trip<100;trip++){for(int i=0;i<10;i++){Check(progress.PickUp(),"Casket fill pickup");progress.RecordPickup(trip*10+i);}Check(progress.Stash()==10,"Casket fill stash");}
            Check(data.casketDucks.Length==1000 && data.carried==0,"Casket capacity");Check(progress.PickUp(),"Inventory after full casket");progress.RecordPickup(1000);Check(progress.Stash()==0 && data.carried==1,"Full casket must retain inventory");
            data.collected=new int[1001];for(int i=0;i<1001;i++)data.collected[i]=i;Check(DuckSaveSystem.Valid(data,game.Settings),"Full casket conservation");
            var root=new GameObject("Moved duck validation");
            try
            {
                var population=root.AddComponent<DuckPopulationManager>();population.Initialize(game.Settings,new SaveData{total=1000,seed=1731},null);
                for(int id=0;id<1000;id++) { Check(population.Detach(id,false),"Detach physical duck");population.Settle(id,new Vector3(20,2,20),Quaternion.identity); }
                Check(population.Remaining==1000 && population.Query(new Vector3(20,3,20),Vector3.down,2,.9f)>=0,"Relocated packed batches and bounds");
                Check(population.Remove(0) && population.Detach(0,true),"Throw collected ID");population.Settle(0,new Vector3(-120,1,250),Quaternion.Euler(25,10,90));
                Check(population.Query(new Vector3(-120,2,250),Vector3.down,2,.9f)==0 && population.Remaining==1000,"Distant relocated query and conservation");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            File.Copy("Temp/DuckValidation/population.csv","Logs/park-population.csv",true);
            File.WriteAllText("Logs/park-checks.txt","PASS: serialized scene and prefab references; expanded terrain; existing economy/save/recovery checks; 1k/10k/50k/100k populations; 1000-duck casket capacity and overflow; relocated batch bounds; throw conservation.");
        }
        public static void ValidateAndBuild(){Run();DuckPrototypeBuilder.BuildValidationPlayer();}
        public static void RefreshAndBuild(){DuckParkBuilder.Build();ValidateAndBuild();}
    }
}
