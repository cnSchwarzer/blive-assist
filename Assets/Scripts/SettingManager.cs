using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI; 

public delegate void UpdateValueCallback<T>(T value, bool isChanged);

public class SmartBool : SmartProperty<bool> {
    public SmartBool() : base(false) { }
    public SmartBool(bool value) : base(value) { }
}

public class SmartProperty<TValue> {
    public SmartProperty() { }

    public SmartProperty(TValue @default) {
        _value = @default;
    }

    [JsonProperty("value")] private TValue _value = default(TValue);
    
    [JsonIgnore]
    public TValue Value {
        get => _value;
        set {
            _value = value;
            OnUpdateValue?.Invoke(value, true); 
        }
    }

    [JsonIgnore] public UpdateValueCallback<TValue> OnUpdateValue { get; private set; }

    public static SmartProperty<TValue> operator +(SmartProperty<TValue> property, UpdateValueCallback<TValue> action) {
        property.OnUpdateValue += action;
        action(property.Value, false);
        return property;
    }
    
    public static SmartProperty<TValue> operator -(SmartProperty<TValue> property, UpdateValueCallback<TValue> action) {
        property.OnUpdateValue -= action;
        return property;
    }

    public static implicit operator TValue(SmartProperty<TValue> v) {
        return v._value;
    } 
    
    public override string ToString() {
        return _value.ToString();
    }
}

public class Settings {
    public SmartProperty<string> RoomId { get; set; } = new("510");
    public SmartProperty<DanmuRingLayout.DanmuRingMode> DanmuRingMode { get; set; } = new(DanmuRingLayout.DanmuRingMode.Ring);
    public SmartProperty<int> DanmuRingColumns { get; set; } = new(2);
    public SmartProperty<bool> DanmuRollShowRepeatOnly { get; set; } = new(false);
    public SmartProperty<bool> DanmuShowRepeat { get; set; } = new(true);
    public SmartProperty<int> DanmuShowRepeatThreshold { get; set; } = new(3);
    public SmartProperty<int> DanmuShowRepeatDuration { get; set; } = new(5);
    public SmartProperty<bool> DanmuShowRepeatSort { get; set; } = new(true);
    public SmartProperty<bool> SuperchatRestore { get; set; } = new(true);
    public SmartProperty<bool> SuperchatListenDelete { get; set; } = new(true);
    public SmartProperty<bool> GiftFreeRestore { get; set; } = new(true);
    public SmartProperty<bool> GiftPaidRestore { get; set; } = new(true);
    public SmartProperty<bool> GiftShowEnterRoom { get; set; } = new(false);
    public SmartProperty<int> GiftPaidThreshold { get; set; } = new(0);
    public SmartProperty<float> SystemLayoutScaleRatio { get; set; } = new(1.25f);
    public SmartProperty<int> SystemFramerate { get; set; } = new(1);
    public SmartProperty<string> SystemLastCloudMessage { get; set; } = new();
}


public class Tutorials {
    public SmartBool FirstShowHelp { get; set; } = new SmartBool();
}

public class SettingManager : MonoBehaviour {  
    public static Settings Settings { get; private set; }
    public static Tutorials Tutorials { get; private set; }
    public static SettingManager Instance { get; private set; }

    public CanvasGroup settingsCanvasGroup; 
    
    public GameObject[] sections;
    public ButtonGroupLayout sectionGroup;

    //Danmu
    public ButtonGroupLayout danmuRingMode;
    public ButtonGroupLayout danmuColumns;
    public Toggle danmuRepeatShow;
    public InputField danmuRepeatThreshold;
    public InputField danmuRepeatDuration;
    public Toggle danmuRepeatSort;
    public Toggle danmuRepeatOnly;

    public Toggle scRestore;
    public Toggle scListenDelete;
    
    public Toggle giftFreeRestore;
    public Toggle giftPaidRestore;
    public Toggle giftEnterRoom;
    public InputField giftPaidThreshold;
    
    public Slider systemLayoutScale;
    public ButtonGroupLayout systemFramerate;

    private bool _initialized = false;
    
    public SettingManager() {
        Instance = this;
    }
    
    public void OnEnable() {
        var saved = PlayerPrefs.GetString("Settings", "{}");
        Debug.Log(saved);
        Settings = JsonConvert.DeserializeObject<Settings>(saved);
        
        saved = PlayerPrefs.GetString("Tutorials", "{}");
        Debug.Log(saved);
        Tutorials = JsonConvert.DeserializeObject<Tutorials>(saved);
    }

    public void Save() {
        if (!_initialized)
            return;
        PlayerPrefs.SetString("Settings", JsonConvert.SerializeObject(Settings));
        PlayerPrefs.SetString("Tutorials", JsonConvert.SerializeObject(Tutorials));
        PlayerPrefs.Save();
    }
    
    private void Start() {
        sectionGroup.OnSelectChanged += i => {
            for (var k = 0; k < sections.Length; ++k) {
                sections[k].SetActive(i == k);
            }
        };
        sectionGroup.SetSelectedAndDispatch(0);
        
        danmuRingMode.OnSelectChanged += i => {
            Settings.DanmuRingMode.Value = (DanmuRingLayout.DanmuRingMode) i;
        };
        danmuColumns.OnSelectChanged += i => {
            Settings.DanmuRingColumns.Value = i;
        };
        danmuRepeatShow.onValueChanged.AddListener(b => {
            Settings.DanmuShowRepeat.Value = b;
        });
        danmuRepeatThreshold.onEndEdit.AddListener(s => {
            if (int.TryParse(s, out var i)) {
                Settings.DanmuShowRepeatThreshold.Value = i;
            }
        });
        danmuRepeatDuration.onEndEdit.AddListener(s => {
            if (int.TryParse(s, out var i)) {
                i = Mathf.Max(i, 1);
                Settings.DanmuShowRepeatDuration.Value = i;
            }
        }); 
        danmuRepeatSort.onValueChanged.AddListener(b => {
            Settings.DanmuShowRepeatSort.Value = b;
        });
        danmuRepeatOnly.onValueChanged.AddListener(b => {
            Settings.DanmuRollShowRepeatOnly.Value = b;
        });
        
        scRestore.onValueChanged.AddListener(b => {
            Settings.SuperchatRestore.Value = b;
        });
        scListenDelete.onValueChanged.AddListener(b => {
            Settings.SuperchatListenDelete.Value = b;
        });
        
        giftFreeRestore.onValueChanged.AddListener(b => {
            Settings.GiftFreeRestore.Value = b;
        });
        giftPaidRestore.onValueChanged.AddListener(b => {
            Settings.GiftPaidRestore.Value = b;
        });
        giftEnterRoom.onValueChanged.AddListener(b => {
            Settings.GiftShowEnterRoom.Value = b;
        });
        giftPaidThreshold.onEndEdit.AddListener(s => {
            if (int.TryParse(s, out var i)) {
                Settings.GiftPaidThreshold.Value = i;
            }
        });
        systemLayoutScale.onValueChanged.AddListener(f => {
            Settings.SystemLayoutScaleRatio.Value = f;
        });
        systemFramerate.OnSelectChanged += i => {
            Settings.SystemFramerate.Value = i;
        };
        
        _initialized = true;
    }

    public void ShowSettings() { 
        StatusBarLayout.Instance.Show(settingsCanvasGroup);
        
        danmuRingMode.SetSelected((int) Settings.DanmuRingMode.Value); 
        danmuColumns.SetSelected(Settings.DanmuRingColumns.Value); 
        danmuRepeatShow.isOn = Settings.DanmuShowRepeat;
        danmuRepeatThreshold.text = Settings.DanmuShowRepeatThreshold.ToString();
        danmuRepeatDuration.text = Settings.DanmuShowRepeatDuration.ToString();
        danmuRepeatSort.isOn = Settings.DanmuShowRepeatSort;
        danmuRepeatOnly.isOn = Settings.DanmuRollShowRepeatOnly;
        
        scRestore.isOn = Settings.SuperchatRestore.Value;
        scListenDelete.isOn = Settings.SuperchatListenDelete.Value;
        
        giftFreeRestore.isOn = Settings.GiftFreeRestore.Value;
        giftPaidRestore.isOn = Settings.GiftPaidRestore.Value;
        giftEnterRoom.isOn = Settings.GiftShowEnterRoom.Value;
        giftPaidThreshold.text = Settings.GiftPaidThreshold.ToString();

        systemLayoutScale.value = Settings.SystemLayoutScaleRatio.Value;
        systemFramerate.SetSelected(Settings.SystemFramerate.Value);
    }

    private void OnApplicationQuit() {
        Save();
    }
}