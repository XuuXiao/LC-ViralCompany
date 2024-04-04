using UnityEngine;

namespace ViralCompany;
public class Camera : GrabbableObject {
    [SerializeField] public float maxLoudness;
    [SerializeField] public float minLoudness;
    [SerializeField] public float minPitch;
    [SerializeField] public float maxPitch;
    private System.Random noisemakerRandom;
    public Animator triggerAnimator;
    void LogIfDebugBuild(string text) {
      #if DEBUG
      Plugin.Logger.LogInfo(text);
      #endif
    }
    public override void Start() {
        base.Start();
    }
    public override void ItemActivate(bool used, bool buttonDown = true) {
        // logic
        triggerAnimator?.SetTrigger("playAnim");
    }
}