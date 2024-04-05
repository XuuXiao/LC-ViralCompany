using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace ViralCompany.Keybinds;
public class IngameKeybinds : LcInputActions {
    [InputAction("<Keyboard>/o", Name = "StopRecord")]
    public InputAction StopRecordKey { get; set; }
    [InputAction("<Keyboard>/p", Name = "StartRecord")]
    public InputAction StartRecordKey { get; set; }
}