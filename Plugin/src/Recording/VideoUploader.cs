using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Text;
using Unity.Netcode;
using ViralCompany.Recording;

namespace ViralCompany.Recording;
internal class VideoUploader : NetworkBehaviour {
    const float DELAY_BETWEEN_PACKETS = 0.5f;

    internal static VideoUploader Instance { get; private set; }

    void Awake() {
        Instance = this;
    }

    void OnDisable() {
        Instance = null;
    }

    internal void UploadClip(RecordedClip clip) {
        
    }
}
