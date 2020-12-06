using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    byte[] flashData;

    public bool isReady { get; private set; }

    // Start is called before the first frame update
    void Awake()
    {
        LoadData();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadData()
    {
        isReady = false;
        string path = Path.Combine(Application.streamingAssetsPath, Config.decryptedExtFlashName);
        try
        {
            flashData = File.ReadAllBytes(path);
            isReady = true;
        }
        catch
        {
            Debug.Log($"Failed to load data, does decrypted SPI NOR image exist at {path}?");
        }
    }

    public byte[] GetAnimation()
    {
        byte[] data = new byte[Config.animationLength];
        Buffer.BlockCopy(flashData, Config.animationOffset, data, 0, data.Length);
        return data;
    }

    public byte[] GetRawCaption(Language lang, int index)
    {
        if (lang == Language.Max) throw new ArgumentOutOfRangeException(nameof(lang));
        int offset;
        int length;
        if (lang == Language.Japanese)
        {
            if (index >= Config.captionTimesJp.Length) throw new ArgumentOutOfRangeException(nameof(index));
            offset = Config.captionOffsetsJp[index];
            length = (Config.captionHeightJp * Config.ANIMATION_WIDTH + 7) / 8;
        }
        else
        {
            if (index >= Config.captionTimesRow.Length) throw new ArgumentOutOfRangeException(nameof(index));
            offset = Config.captionOffsetsRow[(int)lang * Config.captionTimesRow.Length + index];
            length = (Config.captionHeightGeneral * Config.ANIMATION_WIDTH + 7) / 8;
        }

        byte[] data = new byte[length];
        Buffer.BlockCopy(flashData, offset, data, 0, data.Length);
        return data;
    }

    public byte[] GetLanguageBanner(Language lang)
    {
        if (lang == Language.Max) throw new ArgumentOutOfRangeException(nameof(lang));
        int height = Config.langBannerHeight - 2 * Config.langBannerHeightAdjustments[(int)lang];
        int offset = Config.langBannerOffsets[(int)lang];

        byte[] data = new byte[(height * Config.ANIMATION_WIDTH + 7) / 8];
        Buffer.BlockCopy(flashData, offset, data, 0, data.Length);
        return data;
    }

    public byte[] GetAudio()
    {
        byte[] data = new byte[Config.audioLength];
        Buffer.BlockCopy(flashData, Config.audioOffset, data, 0, data.Length);
        return data;
    }
}
