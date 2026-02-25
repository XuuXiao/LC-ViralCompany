using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using System.IO;
using FFMpegCore;
using HarmonyLib;
using ViralCompany.Recording.Encoding;
using ViralCompany.Recording;
using Dawn;
using Dusk;
using Dawn.Utils;
using Unity.Netcode;

namespace ViralCompany;
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(DawnLib.PLUGIN_GUID)]
[BepInDependency(Dusk.MyPluginInfo.PLUGIN_GUID)]
[BepInDependency("com.rune580.LethalCompanyInputUtils")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger = null!;
    internal static IngameKeybinds InputActionsInstance = null!;
    internal class MainAssets(AssetBundle bundle) : AssetBundleLoader<MainAssets>(bundle)
    {
        [LoadFromBundle("UploaderPrefab.prefab")]
        public GameObject UploaderPrefab { get; private set; } = null!; // TODO: Give the prefab the VideoUploader component.
    }

    public static DuskMod Mod { get; private set; } = null!;
    internal static MainAssets Assets { get; private set; } = null!;

    private async void Awake()
    {
        Logger = base.Logger;

	    NetworkVariableSerializationTypes.InitializeSerializer_UnmanagedByMemcpy<CameraItem.RecordState>();
		NetworkVariableSerializationTypes.InitializeEqualityChecker_UnmanagedValueEquals<CameraItem.RecordState>();

        Logger.LogInfo("Ensuring FFmpeg is installed.");
        if (!File.Exists(Path.Combine(FFmpegEncoder.FFmpegInstallPath, "ffmpeg.exe")))
        {
            Logger.LogWarning("FFmpeg is missing! Downloading FFmpeg...");
            Directory.CreateDirectory(FFmpegEncoder.FFmpegInstallPath);
            await YoutubeDLSharp.Utils.DownloadFFmpeg(FFmpegEncoder.FFmpegInstallPath);
        }
        GlobalFFOptions.Configure(options => options.BinaryFolder = FFmpegEncoder.FFmpegInstallPath);

        Logger.LogInfo("Doing patches");
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginInfo.PLUGIN_GUID);

        AssetBundle mainBundle = AssetBundleUtils.LoadBundle(Assembly.GetExecutingAssembly(), "viralcompanyassets");
        Assets = new MainAssets(mainBundle);
        Mod = DuskMod.RegisterMod(this, mainBundle);
        Mod.RegisterContentHandlers();

        GameObject managerObject = new("ViralCompanyDataManager");
        DontDestroyOnLoad(managerObject);
        managerObject.AddComponent<VideoDatabase>();

        InputActionsInstance = new IngameKeybinds();
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }
}