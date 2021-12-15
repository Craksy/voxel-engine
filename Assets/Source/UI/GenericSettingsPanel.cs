using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GenericSettingsPanel : MonoBehaviour
{

    public System.Type ConfigType;

    public GameObject IntFieldPrefab, StringFieldPrefab;

    private List<System.Reflection.FieldInfo> SettingsProperties;
    private Dictionary<System.Reflection.FieldInfo, ISettingsField> fieldmap;
    public Transform ContentPanel;


    public void CreateFields(){
        fieldmap = new Dictionary<System.Reflection.FieldInfo, ISettingsField>();
        var instance = System.Activator.CreateInstance(ConfigType);
        foreach(var p in ConfigType.GetFields().Where(f => f.DeclaringType == ConfigType)){
            if(p.FieldType == typeof(int) || p.FieldType == typeof(float)){
                GameObject obj = Instantiate(IntFieldPrefab, ContentPanel);
                NumberField field = obj.GetComponent<NumberField>();
                field.field = p;
                field._input.text = p.GetValue(instance).ToString();
                fieldmap.Add(p, field);
            }
            else if(p.FieldType == typeof(string)){
                GameObject obj = Instantiate(StringFieldPrefab, ContentPanel);
                StringField field = obj.GetComponent<StringField>();
                field.Label = p.Name;
                fieldmap.Add(p, field);
            }else{
                Debug.Log("Unsupported type: " + p.FieldType.ToString());
            }
        }
    }

    public object GetConfig(){
        var config = System.Activator.CreateInstance(ConfigType);
        foreach(var map in fieldmap){
            map.Key.SetValue(config, map.Value.GetValue());
        }

        return config;
    }
}

public interface ISettingsField{
    object GetValue();
}