if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Use edit mode");
var flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Instance;
System.Func<string,UnityEngine.AudioClip> importMusic=path=>{
 UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
 var importer=(UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(path);
 var sample=importer.defaultSampleSettings;
 sample.loadType=UnityEngine.AudioClipLoadType.Streaming;
 sample.compressionFormat=UnityEngine.AudioCompressionFormat.Vorbis;
 sample.quality=.9f;
 sample.sampleRateSetting=UnityEditor.AudioSampleRateSetting.PreserveSampleRate;
 importer.defaultSampleSettings=sample;importer.forceToMono=false;importer.loadInBackground=true;importer.SaveAndReimport();
 return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(path);
};
var clip=importMusic("Assets/Resources/Audio/Music/Unknown Planet.mp3");
var cutsceneClip=importMusic("Assets/Resources/Audio/Music/Deep Space Pulse.mp3");
var mixerPath="Assets/Resources/Audio/SfxGain.mixer";
var mixer=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>(mixerPath);
var mixerType=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.Audio.AudioMixerController")).First(t=>t!=null);
if(mixer==null)mixer=(UnityEngine.Audio.AudioMixer)mixerType.GetMethod("CreateMixerControllerAtPath",flags).Invoke(null,new object[]{mixerPath});
var group=mixerType.GetProperty("masterGroup",flags).GetValue(mixer);
var snapshot=(UnityEngine.Audio.AudioMixerSnapshot)mixerType.GetProperty("startSnapshot",flags).GetValue(mixer);
var guid=group.GetType().GetMethod("GetGUIDForVolume",flags).Invoke(group,null);
snapshot.GetType().GetMethod("SetValue",flags).Invoke(snapshot,new object[]{guid,6.0205999f});
UnityEditor.EditorUtility.SetDirty(snapshot);UnityEditor.EditorUtility.SetDirty((UnityEngine.Object)group);UnityEditor.EditorUtility.SetDirty(mixer);
var root=UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Resources/SoundManager.prefab");
try{
 var manager=root.GetComponent<SoundManager>();
 var entry=manager.Sounds.First(s=>s.id==SoundIds.BattleMusic);
 entry.clip=clip;entry.volume=1f;
 var cutscene=manager.Sounds.First(s=>s.id==SoundIds.CutsceneMusic);
 cutscene.clip=cutsceneClip;cutscene.volume=1f;
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/SoundManager.prefab");
}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
UnityEditor.AssetDatabase.SaveAssets();
return new{battle=clip.name,battleSeconds=clip.length,cutscene=cutsceneClip.name,cutsceneSeconds=cutsceneClip.length,sfxGainDb=6.0205999f};
