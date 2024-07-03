using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace ViralCompany.Keybinds;
public class IngameKeybinds : LcInputActions {
    [InputAction("<Mouse>/leftButton", Name = "ToggleRecording")]
    public InputAction ToggleRecordingKey { get; set; }

    [InputAction("<Keyboard>/r", Name = "FlipCamera")]
    public InputAction FlipCameraKey { get; set; }
    
    [InputAction("<Keyboard>/q", Name = "ZoomOut")]
    public InputAction ZoomOutLevelKey { get; set; }
    [InputAction("<Keyboard>/e", Name = "ZoomIn")]
    public InputAction ZoomInLevelKey { get; set; }
}