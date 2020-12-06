using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    static readonly int[] indexTable = new int[]
    {
        -4, -3, -2, -1, 2, 4, 8, 16,
        -4, -3, -2, -1, 2, 4, 8, 16
    };

    static readonly int[] stepTable = new int[]
    {
        7, 8, 9, 10, 11, 12, 13, 14, 16, 17,
        19, 21, 23, 25, 28, 31, 34, 37, 41, 45,
        50, 55, 60, 66, 73, 80, 88, 97, 107, 118,
        130, 143, 157, 173, 190, 209, 230, 253, 279, 307,
        337, 371, 408, 449, 494, 544, 598, 658, 724, 796,
        876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066,
        2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358,
        5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
        15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767
    };

    AudioClip clip;
    bool startedPlaying;

    short predictor;
    int stepIndex;

    public MainController mainController;
    public ResourceManager resourceManager;
    public AudioSource source;

    public bool isLoaded { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isLoaded && source.isPlaying != mainController.isPlaying)
        {
            if (mainController.isPlaying)
            {
                if (startedPlaying)
                {
                    source.UnPause();
                }
                else
                {
                    source.Play();
                    startedPlaying = true;
                }
            }
            else
            {
                source.Pause();
            }
        }
    }

    private void OnDisable()
    {
        Unload();
    }

    public void ResetController()
    {
        Unload();
    }

    void Unload()
    {
        if (clip != null) Destroy(clip);
        isLoaded = false;
        startedPlaying = false;
    }

    public void LoadResources()
    {
        if (clip != null) Destroy(clip);
        float[] samples = ImaAdpcmDecompress(resourceManager.GetAudio());
        clip = AudioClip.Create("audio", samples.Length, 1, 48100, false);
        clip.SetData(samples, 0);
        source.clip = clip;
        isLoaded = true;
    }

    float[] ImaAdpcmDecompress(byte[] compressed)
    {
        predictor = 0;
        stepIndex = 40;

        List<float> decompressed = new List<float>();
        foreach (var b in compressed)
        {
            float first = DecompressSample((byte)(b & 0xf));
            float second = DecompressSample((byte)(b >> 4));
            decompressed.Add(first);
            decompressed.Add(first);
            decompressed.Add(second);
            decompressed.Add(second);
        }

        return decompressed.ToArray();
    }

    float DecompressSample(byte sample)
    {
        int step = stepTable[stepIndex];
        int diff = step >> 3;

        if ((sample & 1) != 0) diff += step >> 2;
        if ((sample & 2) != 0) diff += step >> 1;
        if ((sample & 4) != 0) diff += step >> 0;
        if ((sample & 8) != 0) diff = -diff;

        predictor = (short)Saturate(diff + predictor, short.MinValue, short.MaxValue);

        stepIndex += indexTable[sample];
        stepIndex = Saturate(stepIndex, 0, stepTable.Length - 1);

        return Mathf.Clamp((float)predictor / short.MaxValue, -1.0f, 1.0f);
    }

    int Saturate(int value, int min, int max)
    {
        if (value > max) return max;
        if (value < min) return min;
        return value;
    }
}
