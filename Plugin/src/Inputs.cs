using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace ViralCompany.Keybinds;
public class IngameKeybinds : LcInputActions {
    [InputAction("<Mouse>/leftButton", Name = "ToggleRecording")]
    public InputAction ToggleRecordingKey { get; set; }

    [InputAction("<Keyboard>/r", Name = "FlipCamera")]
    public InputAction FlipCameraKey { get; set; }
}