using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Source.Vox;
using Vox.WorldGeneration;

using Vox;

public class CreateMenuScript : MonoBehaviour, IMenuPage
{
    public InputField WorldTxt;
    public Dropdown WorldTypeDropdown;
    public MainMenuController Main;
    public GameObject ProgressPanelPrefab;
    public GameObject SettingsPanelPrefab;

    private System.Type[] WorldTypes = {typeof(HillWorldConfig), typeof(FlatWorldConfig)};
    private string[] TypeNames = {"Hill", "Flat"};

    private GameObject currentType;

    private void Start() {
        WorldTypeDropdown.ClearOptions();
        WorldTypeDropdown.AddOptions(TypeNames.ToList());

        currentType = Instantiate(SettingsPanelPrefab, transform);
        GenericSettingsPanel panel = currentType.GetComponent<GenericSettingsPanel>();
        panel.ConfigType = WorldTypes[0];
        panel.CreateFields();
        WorldTypeDropdown.onValueChanged.AddListener(ChangeType);
    }

    private void ChangeType(int type){
        Destroy(currentType.gameObject);
        currentType = Instantiate(SettingsPanelPrefab, transform);
        var panel = currentType.GetComponent<GenericSettingsPanel>();
        panel.ConfigType = WorldTypes[type];
        panel.CreateFields();
        
    }

    private WorldGenConfig GetConfig(){
        var panel = currentType.GetComponent<GenericSettingsPanel>();
        var config = (WorldGenConfig)panel.GetConfig();
        config.WorldName = FormatWorldName(WorldTxt.text);
        config.WorldType = TypeNames[WorldTypeDropdown.value];
        return config;
    }

    public async void CreateWorld(){
        var conf = GetConfig();
        Action<World, Vector2Int, WorldGenConfig> genFunc;
        
        Debug.Log("Create world");
        switch(conf.WorldType){
            case "Hill":
                genFunc = WorldGeneration.GenerateSurface2;
                break;
            case "Flat":
                genFunc = WorldGeneration.GenerateRandom;
                break;
            default:
                Debug.Log("Invalid world type");
                return;
        }

        var canvasGroup = GetComponent<CanvasGroup>();

        var savepath = Path.Combine(SaveManager.SavesBasePath, conf.WorldName);
        Debug.Log("Saving to " + savepath);
        if(Directory.Exists(savepath)){
            Debug.Log("World already exists");
            return;
        }
        Directory.CreateDirectory(savepath);
        SaveManager.CurrentSavePath = savepath;

        var ppanelObj  = Instantiate(ProgressPanelPrefab, transform, true);
        canvasGroup.interactable = false;
        var ppanel = ppanelObj.GetComponent<ProgressPanel>();
        ((RectTransform)ppanelObj.transform).offsetMin = new Vector2(50,50);
        ((RectTransform)ppanelObj.transform).offsetMax = new Vector2(-50,-50);

        var progress = new Progress<WorldGenerationProgress>(report => {
            var percent = report.CompletedChunks/(float)report.TotalChunks;
            ppanel.UpdateProgress(percent, report.Message);
        });

        await GenerateWorldAsync(progress,new Vector2Int(8, 8), conf, genFunc);
        //await TestGenerate(progress, 100, 100);
        Debug.Log("Done");
        canvasGroup.interactable = true;
        Destroy(ppanelObj);
    }

    private string FormatWorldName(string name){
        return name.Trim(' ').Replace(' ', '_');
    }
    public void SwitchTo()
    {
    }

    private async Task GenerateWorldAsync(
        IProgress<WorldGenerationProgress> progress, Vector2Int size, WorldGenConfig conf,
        Action<World, Vector2Int, WorldGenConfig> generator){
        Debug.Log($"Generating world {size} big");
        Debug.Log("chunk size is " + GridManager.ChunkShape);
        var world = new World(); 
        var totalChunks = size.x*size.y*5;
        var completed = 0;
        await Task.Run(() => {
            for(var x = 0; x<size.x; x++){
                for(var z = 0; z<size.y; z++){
                    Debug.Log("chunk " + x + "," + z);
                    for(var y = 0; y<5;y++){
                        world.LoadChunk(new Vector3Int(x,y,z));
                    }
                    generator(world, new Vector2Int(x,z), conf);

                    for(var y = 0; y<5;y++){
                        world.Unload(new Vector3Int(x,y,z));
                    }
                    completed += 5;
                    progress.Report(new WorldGenerationProgress
                        {CompletedChunks = completed, TotalChunks = totalChunks, Message = "Generating surface..."});
                }
            }
        });
    }
}

[Serializable]
public struct WorldTypePrefab{
    public string Name;
    public GameObject Prefab;
}

public class WorldGenerationProgress{
    public int CompletedChunks {get; set; }
    public int TotalChunks {get; set; }
    public string Message {get; set; }
}