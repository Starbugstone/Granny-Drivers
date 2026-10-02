using System;
using System.Collections.Generic;
using System.IO;
using GrannyRacer.Racing;
using GrannyRacer.UI;
using GrannyRacer.Walker;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace GrannyRacer.Editor
{
    public static class NeighborhoodTourBuilder
    {
        public const string Kit="Assets/GrannyRacer/Art/Environment/Neighborhood";
        public const string AudioRoot="Assets/GrannyRacer/Audio/SFX/Arcade";
        public const string MapleRecipe="Assets/GrannyRacer/Settings/Circuit_MapleGardens.asset";
        public const string SchoolRecipe="Assets/GrannyRacer/Settings/Circuit_SchoolRun.asset";
        public const string MapleScene="Assets/GrannyRacer/Scenes/Tracks/Circuit_MapleGardens.unity";
        public const string SchoolScene="Assets/GrannyRacer/Scenes/Tracks/Circuit_SchoolRun.unity";
        public static CircuitDestination[] Destinations()=>new[] {
            new CircuitDestination { title="Sunday Block Party",scenePath=CircuitSceneBuilder.DefaultScenePath,description="Broad blocks and a narrow driveway cut." },
            new CircuitDestination { title="Maple Gardens",scenePath=MapleScene,description="Porch-lined doglegs and a neighborhood garden." },
            new CircuitDestination { title="School Run",scenePath=SchoolScene,description="A longer loop with two inward corners and a school playground." }};

        [MenuItem("Granny Racer/Circuit Builder/Build Neighborhood Tour")]
        public static void BuildTour()
        {
            Import();
            CreateRecipe(false);CreateRecipe(true);
            CircuitSceneBuilder.BuildDefault();
            CircuitSceneBuilder.Build(AssetDatabase.LoadAssetAtPath<CircuitRecipe>(MapleRecipe),MapleScene);
            CircuitSceneBuilder.Build(AssetDatabase.LoadAssetAtPath<CircuitRecipe>(SchoolRecipe),SchoolScene);
            var scenes=new List<EditorBuildSettingsScene>();
            foreach(var destination in Destinations())scenes.Add(new EditorBuildSettingsScene(destination.scenePath,true));
            foreach(var scene in EditorBuildSettings.scenes)
                if(scene.path!=MapleScene && scene.path!=SchoolScene && scene.path!=CircuitSceneBuilder.DefaultScenePath)scenes.Add(scene);
            EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();
            Debug.Log("[GrannyRacer] Built three selectable circuits with original arcade effects. Human review pending.");
        }
        private static void CreateRecipe(bool school)
        {
            var path=school?SchoolRecipe:MapleRecipe;
            if(AssetDatabase.LoadAssetAtPath<CircuitRecipe>(path)!=null)return; // Authored recipes survive rebuilds.
            var recipe=ScriptableObject.CreateInstance<CircuitRecipe>();
            recipe.circuitName=school?"SCHOOL RUN":"MAPLE GARDENS";
            recipe.seed=school?73:41;recipe.controlPoints=CircuitLayout.Neighborhood(school);
            recipe.lotSpacing=23;recipe.houseSetback=17;recipe.neighborhoodDetails=true;
            var palette=new List<CircuitAssetEntry>(CircuitSceneBuilder.DefaultPalette());
            palette.Add(new CircuitAssetEntry { label="Porch house",prefab=Model("ENV_PorchHouse"),role=CircuitAssetRole.House });
            recipe.palette=palette.ToArray();
            var extras=new List<CircuitPlacement>();
            if(school)
            {
                extras.Add(Extra("ENV_NeighborhoodSchool",0,-60,180));
                extras.Add(Extra("PRP_PlaygroundSlide",22,-52,90));
                extras.Add(Extra("PRP_Seesaw",22,-42,0));
                extras.Add(Extra("PRP_PicnicTable",-18,-52,0));
            }
            else
            {
                extras.Add(Extra("PRP_PlaygroundSlide",-12,-40,0));
                extras.Add(Extra("PRP_Seesaw",-22,-40,0));
                extras.Add(Extra("PRP_PicnicTable",-12,-30,0));
                extras.Add(Extra("PRP_PicnicTable",-22,-30,0));
            }
            extras.Add(Extra("PRP_WindSpinner",school?-17:-28,school?-58:-35,0));
            recipe.extraAssets=extras.ToArray();AssetDatabase.CreateAsset(recipe,path);AssetDatabase.SaveAssets();
        }
        private static CircuitPlacement Extra(string name,float x,float z,float yaw)=>new CircuitPlacement { prefab=Model(name),position=new Vector3(x,0,z),yaw=yaw };
        private static GameObject Model(string name)=>AssetDatabase.LoadAssetAtPath<GameObject>(Kit+"/"+name+".fbx");

        public static void Import()
        {
            const string stage="Blender/Exports/Neighborhood";
            if(!File.Exists(stage+"/manifest.json"))throw new FileNotFoundException("Generate the neighborhood Blender kit first.");
            CircuitSceneBuilder.EnsureFolder(Kit);
            foreach(var file in Directory.GetFiles(stage))
                if(file.EndsWith(".fbx") || file.EndsWith(".png"))File.Copy(file,Kit+"/"+Path.GetFileName(file),true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var texturePath=Kit+"/NeighborhoodPalette.png";
            var texture=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            texture.sRGBTexture=true;texture.mipmapEnabled=false;texture.filterMode=FilterMode.Point;
            texture.textureCompression=TextureImporterCompression.Uncompressed;texture.SaveAndReimport();
            var mat=CircuitRoadBuilder.Material(Kit,"MAT_NeighborhoodPalette",Color.white);
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));EditorUtility.SetDirty(mat);
            var audit=new List<Imported>();
            foreach(var file in Directory.GetFiles(Kit,"*.fbx"))
            {
                var path=file.Replace('\\','/');var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                var animated=path.Contains("WindSpinner") || path.Contains("Seesaw");
                importer.globalScale=1;importer.useFileUnits=true;importer.bakeAxisConversion=false;
                importer.importCameras=false;importer.importLights=false;importer.addCollider=false;importer.isReadable=false;
                importer.meshCompression=ModelImporterMeshCompression.Off;
                importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
                importer.animationType=animated?ModelImporterAnimationType.Legacy:ModelImporterAnimationType.None;
                importer.importAnimation=animated;importer.animationCompression=ModelImporterAnimationCompression.Off;
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"MAT_NeighborhoodPalette"),mat);
                importer.SaveAndReimport();
                if(animated)
                {
                    var clips=importer.defaultClipAnimations;
                    foreach(var clip in clips) { clip.loopTime=true;clip.wrapMode=WrapMode.Loop; }
                    importer.clipAnimations=clips;importer.SaveAndReimport();
                }
                var entry=new Imported { asset=path,dimensions=CircuitSceneBuilder.ModelBounds(AssetDatabase.LoadAssetAtPath<GameObject>(path),1,0).size };
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if(asset is Mesh mesh)for(var i=0;i<mesh.subMeshCount;i++) { entry.triangles+=(long)mesh.GetIndexCount(i)/3;entry.materialSlots++; }
                    if(asset is AnimationClip clip && !clip.name.StartsWith("__preview")) { entry.animationClips++;entry.animationSeconds=clip.length; }
                }
                if(animated && entry.animationClips==0)throw new InvalidOperationException("No baked Blender animation imported: "+path);
                audit.Add(entry);
            }
            foreach(var file in Directory.GetFiles(AudioRoot,"*.wav"))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
                importer.forceToMono=true;
                var settings=importer.defaultSampleSettings;settings.preloadAudioData=true;settings.loadType=AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            }
            File.WriteAllText("Docs/Art/NEIGHBORHOOD_IMPORT_AUDIT.json",JsonUtility.ToJson(new ImportReport { assets=audit.ToArray() },true));
            File.Copy(stage+"/manifest.json","Docs/Art/NEIGHBORHOOD_EXPORT_MANIFEST.json",true);
            AssetDatabase.SaveAssets();
        }

        public static void AddTourAndAudio()
        {
            // A builder can still be used independently before the expansion has been imported.
            if(!File.Exists(AudioRoot+"/Go.wav"))return;
            var walker=Object.FindAnyObjectByType<ArcadeWalkerController>();
            var race=Object.FindAnyObjectByType<RaceController>();
            var hud=Object.FindAnyObjectByType<DrivingDebugHud>();
            hud.gameObject.AddComponent<CircuitTour>().Configure(Destinations());
            var bank=new AudioClip[9];
            for(var i=0;i<bank.Length;i++)bank[i]=Clip(((ArcadeAudioPresentation.Cue)i).ToString());
            var audio=walker.gameObject.AddComponent<ArcadeAudioPresentation>();
            audio.Configure(walker,race,Source(walker.transform,"Race and impact cues",null,false),
                Source(walker.transform,"Walker wheels",Clip("Roll"),true),Source(walker.transform,"Slipper skid",Clip("Skid"),true),
                Source(walker.transform,"Rocket loop",Clip("Rocket"),true),Source(walker.transform,"Garden ambience",Clip("Garden"),true),bank);
        }
        private static AudioClip Clip(string name)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot+"/"+name+".wav");
            if(clip==null)throw new FileNotFoundException("Missing arcade sound: "+name);
            return clip;
        }
        private static AudioSource Source(Transform parent,string name,AudioClip clip,bool loop)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);var source=obj.AddComponent<AudioSource>();
            source.clip=clip;source.loop=loop;source.playOnAwake=false;source.spatialBlend=0;source.dopplerLevel=0;
            source.volume=loop?0:1;source.priority=loop?160:32;return source;
        }
        public static void AddNeighborhood(CircuitRecipe recipe,TrackDefinition track,Transform root,string folder)
        {
            var occupied=new List<Bounds>();
            var houses=new List<Transform>();
            for(var i=0;i<root.childCount;i++)
            {
                var child=root.GetChild(i);
                if(child.GetComponentsInChildren<Renderer>().Length==0)continue;
                if(!child.name.Contains("Dash") && !child.name.Contains("Gate"))occupied.Add(CircuitSceneBuilder.RendererBounds(child.gameObject));
                if(child.name.Contains("House") || child.name.Contains("Garage"))houses.Add(child);
            }
            var paving=CircuitRoadBuilder.Material(folder,"Lot paths",new Color(.60f,.59f,.51f));
            // Connect the homes to their actual nearest sidewalk, keeping cars in private drives.
            for(var h=0;h<houses.Count;h++)
            {
                var house=houses[h];var nearest=Vector3.zero;var best=float.PositiveInfinity;var width=0f;
                for(var i=0;i<track.Waypoints.Length;i++)
                {
                    var a=track.Waypoints[i];var b=track.Waypoints[(i+1)%track.Waypoints.Length];var span=b.position-a.position;
                    var point=a.position+span*Mathf.Clamp01(Vector3.Dot(house.position-a.position,span)/span.sqrMagnitude);
                    var d=(point-house.position).sqrMagnitude;if(d>=best)continue;
                    best=d;nearest=point;width=Mathf.Max(a.width,b.width);
                }
                var outward=(house.position-nearest).normalized;
                var begin=nearest+outward*(width*.5f+3.3f);
                var end=house.position-outward*4.5f;var center=(begin+end)*.5f;var length=Vector3.Distance(begin,end);
                if(length<1 || !CircuitLayout.ClearsRoad(center,Mathf.Sqrt(length*length*.25f+2.25f),track.Waypoints,0))continue;
                var path=GameObject.CreatePrimitive(PrimitiveType.Cube);path.name="Private driveway";path.transform.SetParent(root,false);
                path.transform.SetPositionAndRotation(center+Vector3.up*.004f,Quaternion.LookRotation(outward));
                path.transform.localScale=new Vector3(3,.012f,length);path.GetComponent<Renderer>().sharedMaterial=paving;
                Object.DestroyImmediate(path.GetComponent<Collider>());path.isStatic=true;
                if(h%3==0)TryPlace(Model("PRP_StationWagon"),center,Quaternion.LookRotation(outward).eulerAngles.y,root,track,recipe,occupied);
                if(h%7==0)TryPlace(Model("PRP_WindSpinner"),house.position+Vector3.Cross(Vector3.up,outward)*7,0,root,track,recipe,occupied);
            }
            // Back streets are suggested by coherent parallel residential rows beyond the racing lots.
            var porch=Model("ENV_PorchHouse");
            for(var x=-180;x<=180;x+=25)
            for(var z=-140;z<=140;z+=26)
            {
                // Leave the authored communal park/school lawn open.
                if(x>-40 && x<40 && z>-85 && z<0)continue;
                TryPlace(porch,new Vector3(x,0,z),z<0?180:0,root,track,recipe,occupied);
            }
            if(recipe.extraAssets.Length==0)return;
            var label=new GameObject("Neighborhood landmark sign");label.transform.SetParent(root,false);
            label.transform.SetPositionAndRotation(recipe.extraAssets[0].position+new Vector3(0,4.1f,-4.3f),Quaternion.Euler(0,180,0));
            var text=label.AddComponent<TextMesh>();text.text=recipe.circuitName=="SCHOOL RUN"?"MAPLE ELEMENTARY":"MAPLE COMMUNITY GARDEN";
            text.fontSize=64;text.characterSize=.08f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.98f,.90f,.68f);
        }
        private static void TryPlace(GameObject model,Vector3 position,float yaw,Transform parent,TrackDefinition track,CircuitRecipe recipe,List<Bounds> occupied)
        {
            var bounds=CircuitSceneBuilder.ModelBounds(model,1,yaw);bounds.center+=position;
            var radius=new Vector2(bounds.extents.x,bounds.extents.z).magnitude;
            if(!CircuitLayout.ClearsRoad(new Vector3(bounds.center.x,0,bounds.center.z),radius,track.Waypoints)
                || !CircuitLayout.ClearsShortcut(position,radius,recipe))return;
            var padded=bounds;padded.Expand(2);
            foreach(var other in occupied)if(padded.Intersects(other))return;
            var wrapper=new GameObject(model.name+" Lot");wrapper.transform.SetParent(parent,false);
            wrapper.transform.SetPositionAndRotation(position+Vector3.up*(position.y-bounds.min.y),Quaternion.Euler(0,yaw,0));
            PrefabUtility.InstantiatePrefab(model,wrapper.transform);
            var animated=wrapper.GetComponentsInChildren<Animation>().Length>0;
            foreach(var child in wrapper.GetComponentsInChildren<Transform>())child.gameObject.isStatic=!animated;
            foreach(var animation in wrapper.GetComponentsInChildren<Animation>()) { animation.playAutomatically=true;animation.wrapMode=WrapMode.Loop; }
            occupied.Add(bounds);
        }
        [Serializable] private sealed class Imported { public string asset;public long triangles;public int materialSlots;public Vector3 dimensions;public int animationClips;public float animationSeconds; }
        [Serializable] private sealed class ImportReport { public Imported[] assets;public bool humanReviewed=false; }
    }
}
