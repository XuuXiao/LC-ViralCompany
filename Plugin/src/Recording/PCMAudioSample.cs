using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using FFMpegCore.Pipes;

namespace ViralCompany.src.Recording;
internal class PCMAudioSample : IAudioSample {
    internal readonly byte[] _sample;

    public PCMAudioSample(byte[] sample) {
        _sample = sample;
    }

    public void Serialize(Stream stream) {
        stream.Write(_sample, 0, _sample.Length);
    }

    public async Task SerializeAsync(Stream stream, CancellationToken token) {
        Plugin.Logger.LogDebug($"a: {stream.CanWrite}, , c: {_sample.Length}");
        await stream.WriteAsync(_sample, 0, _sample.Length, token).ConfigureAwait(false);
    }
}
