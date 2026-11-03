using System;
using System.IO;
using System.Linq;
using Survivor.Combat;
using Survivor.Data;
using Survivor.Player;
using Survivor.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Survivor.Editor
{
    public static class M9ProjectSetup
    {
        [MenuItem("Survivor/Create M9 Presentation")]
        public static void CreateAssets()
        {
            if(EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play Mode and save scene edits first");
            var sprites=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Generated/Characters.png").OfType<Sprite>().ToArray();
            Sprite Get(string name)=>sprites.Single(s=>s.name==name);
            foreach(var name in new[]{"Melee","Ranged","Bomber","Elite","Boss"})
            {
                var definition=AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Data/Battle/"+name+".asset");
                if(definition==null)throw new InvalidOperationException("Missing enemy profile: "+name);
                definition.sprite=Get(name);definition.color=Color.white;EditorUtility.SetDirty(definition);
            }
            const string playerPath="Assets/Prefabs/Player/Player.prefab";
            var player=PrefabUtility.LoadPrefabContents(playerPath);
            try {var actor=player.GetComponent<PlayerActor>();actor.body.sprite=Get("Hero");actor.tint=Color.white;actor.body.color=Color.white;if(player.GetComponent<CharacterMotion>()==null)player.AddComponent<CharacterMotion>();PrefabUtility.SaveAsPrefabAsset(player,playerPath);}
            finally{PrefabUtility.UnloadPrefabContents(player);}
            const string enemyPath="Assets/Prefabs/Enemies/Melee.prefab";var enemy=PrefabUtility.LoadPrefabContents(enemyPath);
            try{if(enemy.GetComponent<CharacterMotion>()==null)enemy.AddComponent<CharacterMotion>();PrefabUtility.SaveAsPrefabAsset(enemy,enemyPath);}
            finally{PrefabUtility.UnloadPrefabContents(enemy);}
            const string floorPath="Assets/Art/Generated/Floor.png";
            File.Copy("Documentation/ArtSources/Floor-v1.png",floorPath,true);AssetDatabase.ImportAsset(floorPath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(floorPath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=128;var textureSettings=new TextureImporterSettings();importer.ReadTextureSettings(textureSettings);textureSettings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(textureSettings);importer.wrapMode=TextureWrapMode.Repeat;
            importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.SaveAndReimport();
            var flow=AssetDatabase.LoadAssetAtPath<FlowConfig>("Assets/Data/Flow/FlowConfig.asset");flow.audio=M9AudioBuilder.Create();EditorUtility.SetDirty(flow);
            var burst=CreateBurst();AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var world=Object.FindFirstObjectByType<BattleWorld>();world.burstPrefab=burst;
            var floor=GameObject.Find("Arena/Floor").GetComponent<SpriteRenderer>();floor.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(floorPath);
            floor.transform.localScale=Vector3.one;floor.color=new Color(0.6f,0.65f,0.65f);floor.drawMode=SpriteDrawMode.Tiled;floor.size=new Vector2(48,30);
            foreach(Transform child in floor.transform.parent)if(child.name=="GridX" || child.name=="GridY")child.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var old=GameObject.Find("MenuArtwork");if(old!=null)Object.DestroyImmediate(old);var art=new GameObject("MenuArtwork");
            for(int i=0;i<2;i++)
            {
                var go=new GameObject(i==0?"Hero":"Warden",typeof(SpriteRenderer));go.transform.SetParent(art.transform,false);
                go.transform.position=Camera.main.ViewportToWorldPoint(new Vector3(i==0?0.08f:0.92f,0.48f,10));var renderer=go.GetComponent<SpriteRenderer>();
                renderer.sprite=Get(i==0?"Hero":"Boss");renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlaceholderUnlit.mat");
                renderer.color=new Color(0.6f,0.7f,0.75f,0.8f);renderer.sortingOrder=-5;go.transform.localScale=Vector3.one*1.8f;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        private static BurstActor CreateBurst()
        {
            const string materialPath="Assets/Materials/Burst.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(material,materialPath);}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Placeholders/Circle.png"));
            material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;EditorUtility.SetDirty(material);
            var root=new GameObject("Burst",typeof(BurstActor));var visual=new GameObject("Particles",typeof(ParticleSystem));visual.transform.SetParent(root.transform,false);
            var particles=visual.GetComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.loop=false;main.playOnAwake=false;main.duration=0.5f;main.startLifetime=0.45f;main.startSize=0.09f;main.maxParticles=32;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=particles.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,12)});
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=0.12f;
            var size=particles.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
            var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=20;
            root.GetComponent<BurstActor>().particles=particles;root.SetActive(false);
            const string path="Assets/Prefabs/Combat/Burst.prefab";PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<BurstActor>();
        }
    }
}
