using FFMpegCore.Pipes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ViralCompany.Recording;

// why?????????????
internal class Texture2DVideoFrame(Texture2D texture) : IVideoFrame {
    public int Width => Source.width;

    public int Height => Source.height;

    public string Format { get; private set; } = ConvertStreamFormat(texture.format);

    public Texture2D Source { get; private set; } = texture ?? throw new ArgumentNullException(nameof(texture));

    public void Serialize(Stream stream) {
        var data = Source.GetRawTextureData();
        stream.Write(data, 0, data.Length);
    }

    public async Task SerializeAsync(Stream stream, CancellationToken token) {
        var data = Source.GetRawTextureData();
        await stream.WriteAsync(data, 0, data.Length, token).ConfigureAwait(false);
    }

    private static string ConvertStreamFormat(TextureFormat format) {
        return "rgb24";
    }
}
