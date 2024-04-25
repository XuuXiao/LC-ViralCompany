using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ViralCompany.Recording;
internal class WavWriter {
    private int bufferSize;
    private int numBuffers;
    private int outputRate = 44100;
    private string fileName;
    private int headerSize = 44; // default for uncompressed wav
    public int Channels = 2;

    private FileStream fileStream;

    internal WavWriter(string path) {

        outputRate = AudioSettings.outputSampleRate;
        AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);

        fileStream = new FileStream(path, FileMode.Create);
        byte emptyByte = new byte();

        for(int i = 0; i < headerSize; i++) // preparing the header
        {
            fileStream.WriteByte(emptyByte);
        }
    }

    internal void WriteStereoAudio(float[] dataSource) {
        if(fileStream == null) {
            throw new IOException("WavWriter is CLOSED! Don't write to it!");
        }
        Int16[] intData = new Int16[dataSource.Length];
        // converting in 2 steps: float[] to Int16[], // then Int16[] to Byte[]

        byte[] bytesData = new byte[dataSource.Length * 2];
        // bytesData array is twice the size of
        // dataSource array because a float converted in Int16 is 2 bytes.

        int rescaleFactor = 32767; // to convert float to Int16

        for(int i = 0; i < dataSource.Length; i++) {
            intData[i] = (short)(dataSource[i] * rescaleFactor);
            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    internal void WriteMonoAudio(float[] dataSource) {
        if(fileStream == null) {
            throw new IOException("WavWriter is CLOSED! Don't write to it!");
        }
        // Prepare stereo data (twice the length of mono)
        Int16[] intData = new Int16[dataSource.Length * 2];
        byte[] bytesData = new byte[dataSource.Length * 4]; // Each sample is 2 bytes, so times 2 for stereo, times 2 again for bytes

        int rescaleFactor = 32767; // to convert float to Int16

        for(int i = 0; i < dataSource.Length; i++) {
            // Convert mono to stereo by duplicating the sample
            short sample = (short)(dataSource[i] * rescaleFactor);

            // Assign the same sample to both left and right channels
            intData[i * 2] = sample; // Left channel
            intData[i * 2 + 1] = sample; // Right channel

            // Convert each sample to bytes and copy to bytesData
            byte[] byteArr = BitConverter.GetBytes(sample);
            byteArr.CopyTo(bytesData, i * 4); // Multiply by 4 because each sample is 2 bytes and each channel has 2 bytes
            byteArr.CopyTo(bytesData, i * 4 + 2); // Copy to the next channel
        }

        // Write stereo data to file stream
        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    internal void Close() {
        fileStream.Seek(0, SeekOrigin.Begin);

        byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        ushort one = 1;

        byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        byte[] numChannels = BitConverter.GetBytes((ushort)2);
        Plugin.Logger.LogDebug("numChannels as byte[]: " + string.Join(", ", numChannels));
        fileStream.Write(numChannels, 0, 2);

        byte[] sampleRate = BitConverter.GetBytes(outputRate);
        fileStream.Write(sampleRate, 0, 4);

        byte[] byteRate = BitConverter.GetBytes(outputRate * 2 * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
        fileStream.Write(byteRate, 0, 4);

        ushort four = 4;
        byte[] blockAlign = BitConverter.GetBytes(four);
        fileStream.Write(blockAlign, 0, 2);

        ushort sixteen = 16;
        byte[] bitsPerSample = BitConverter.GetBytes(sixteen);
        fileStream.Write(bitsPerSample, 0, 2);

        byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(dataString, 0, 4);

        byte[] subChunk2 = BitConverter.GetBytes(fileStream.Length - headerSize);
        fileStream.Write(subChunk2, 0, 4);

        fileStream.Close();
        fileStream = null;
    }
}
