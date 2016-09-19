using ff14bot.Helpers;
using System.IO;
using System.Configuration;
using System.ComponentModel;
using Newtonsoft.Json;

public class SparrowSettings : JsonSettings
{
    [JsonIgnore]
    private static SparrowSettings _instance;
    public static SparrowSettings Instance { get { return _instance ?? (_instance = new SparrowSettings("SparrowSettings")); } }
    public SparrowSettings(string filename) : base(Path.Combine(CharacterSettingsDirectory, "Sparrow.json")) { }

    [Setting, DefaultValue("Free")]
    public string Stance { get; set; }

    [Setting, DefaultValue("None")]
    public string Feed { get; set; }

    [Setting, DefaultValue(25)]
    public double OnlyFeedWhenTimeAbove { get; set; }
}
