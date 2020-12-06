using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.IO;
using SixLabors.ImageSharp.Memory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

public class AnimationDecoder : MonoBehaviour
{
    BufferedReadStream srcStream;
    BinaryReader br;
    byte[,] drawBuffer;
    Buffer2D<byte> decodeBuffer;
    byte[] cachedCaption;
    Language cachedLang;
    int cachedCaptionIndex;
    int decodingState;
    bool decodingActiveValue;
    MemoryAllocator memoryAllocator;

    // GIF stuff
    ushort width;
    ushort height;
    byte minCodeSize;

    public MainController mainController;
    public ResourceManager resourceManager;

    public int framesDecoded { get; private set; }
    public bool decodingActive
    {
        get
        {
            return decodingActiveValue;
        }
        set
        {
            if (srcStream != null && framesDecoded < Config.numFrames)
            {
                decodingActiveValue = value;
            }
            else
            {
                decodingActiveValue = false;
            }
        }
    }
    public byte[,] indexedPixels => drawBuffer;

    // Start is called before the first frame update
    void Start()
    {
        memoryAllocator = new ArrayPoolMemoryAllocator();
        drawBuffer = new byte[Config.ANIMATION_HEIGHT, Config.ANIMATION_WIDTH];
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDisable()
    {
        ResetController();
    }

    public void AnimUpdate()
    {
        switch (decodingState)
        {
            case 0:
                // Decode frame
                // In actual firmware 1/4 of the frame is decoded, but we're fast enough on a PC,
                // so decode the entire thing and skip decoding for the next 3 frames
                // (timing shouldn't be affected due to using fixed update)
                Profiler.BeginSample("GIF decode frame");
                DecodeFrame();
                Profiler.EndSample();
                break;
            case 1:
            case 2:
            case 3:
                break;
            case 4:
                // Transfer decoded frame to buffer and apply captions
                FinishFrame();
                break;
        }

        ++decodingState;
        if (decodingState > 4)
        {
            ++framesDecoded;
            decodingState = 0;
            decodingActive = false;
        }
    }

    public void ResetController()
    {
        decodingActive = false;
        if (srcStream != null)
        {
            srcStream.Dispose();
            br = null;
            srcStream = null;
        }
        if (decodeBuffer != null)
        {
            decodeBuffer.Dispose();
            decodeBuffer = null;
        }
        if (drawBuffer != null)
        {
            Array.Clear(drawBuffer, 0, drawBuffer.Length);
        }
    }

    public void LoadResources()
    {
        srcStream = new BufferedReadStream(new Configuration(), new MemoryStream(resourceManager.GetAnimation()));
        br = new BinaryReader(srcStream);

        br.ReadBytes(6); // Skip signature and version
        width = br.ReadUInt16();
        height = br.ReadUInt16();
        byte packed = br.ReadByte();
        br.ReadBytes(2); // skip the rest of the header
        int colorDepth = (packed & 3) + 1;
        if ((packed & 0x80) != 0)
        {
            int gctSize = 1 << colorDepth;
            br.ReadBytes(gctSize * 3); // ignore GCT since it's overridden by custom palette
        }

        minCodeSize = br.ReadByte();
        decodingActive = true;
        decodingState = 0;
        framesDecoded = 0;
        // Assuming here GIF width and height matches what we have hardcoded, but in reality
        // the firmware treats the buffer as 1-dimensional, so if you change the GIF size
        // it'll look funny on firmware but crash here
        decodeBuffer = memoryAllocator.Allocate2D<byte>(new Size(width, height));

        cachedLang = Language.Max;
        cachedCaption = null;
        cachedCaptionIndex = -1;
    }

    void DecodeFrame()
    {
        using (LzwDecoder decoder = new LzwDecoder(memoryAllocator, srcStream))
            decoder.DecodePixels(minCodeSize, decodeBuffer);
        //Debug.Log($"Frame {framesDecoded}, Decode pos: 0x{srcStream.Position:x6}");
        br.ReadByte(); // skip frame terminator
    }

    void FinishFrame()
    {
        // Apply decoded frames
        for (int y = 0; y < height; ++y)
        {
            var row = decodeBuffer.GetRowSpan(y);
            for (int x = 0; x < width; ++x)
            {
                drawBuffer[y, x] ^= row[x];
            }
        }

        // Note at this point we haven't updated the frame counter for the current frame
        if (framesDecoded == 8)
        {
            // Clear banner so frame result for fade is correct
            // But why is it here? Banner is drawn in by the main controller, not animation controller
            for (int y = 0; y < Config.langBannerHeight; ++y)
                for (int x = 0; x < Config.ANIMATION_WIDTH; ++x)
                {
                    drawBuffer[y, x] = 2;
                }
        }
        else if (framesDecoded == 13)
        {
            // Clear last line in buffer
            // Presumably no caption covers this spot?
            for (int x = 0; x < Config.ANIMATION_WIDTH; ++x)
            {
                drawBuffer[Config.ANIMATION_HEIGHT - 1, x] = 1;
            }
        }
        else if (framesDecoded > 17)
        {
            // Render captions
            int y = Config.ANIMATION_HEIGHT - 1 - Config.captionHeightGeneral;

            // Note: even though these two cases do mostly the same things, there are very slight differences
            // between how they do them. For example, JP's caption clear uses a slightly different height than
            // ROW
            if (mainController.currentLanguage == Language.Japanese)
            {
                int captionIndex;
                for (captionIndex = 0; captionIndex < Config.captionTimesJp.Length; ++captionIndex)
                {
                    if (framesDecoded < Config.captionTimesJp[captionIndex]) break;
                }
                if (captionIndex >= Config.captionTimesJp.Length)
                    captionIndex = Config.captionTimesJp.Length - 1;
                int captionTime = Config.captionTimesJp[captionIndex];

                // Caching is not in the firmware because if can access flash directly as memory
                if (cachedLang != mainController.currentLanguage || cachedCaptionIndex != captionIndex || cachedCaption == null)
                {
                    cachedLang = mainController.currentLanguage;
                    cachedCaptionIndex = captionIndex;
                    cachedCaption = resourceManager.GetRawCaption(cachedLang, captionIndex);
                }

                if (captionTime - framesDecoded <= 2)
                {
                    // Clear caption area for the last two frames of current caption
                    int endY = y + Config.captionHeightGeneral + 1; // It's actually 2 in firmware; why did they overflow?
                    for (; y < endY; ++y)
                        for (int x = 0; x < Config.ANIMATION_WIDTH; ++x)
                        {
                            drawBuffer[y, x] = Config.captionBgColor;
                        }
                }
                else
                {
                    byte b = 0;
                    byte bitsRemaining = 0;
                    int i = 0;
                    var fgColor = captionIndex == Config.captionTimesJp.Length - 1 ? Config.captionLastColor : Config.captionFgColor;
                    for (int yOff = 0; yOff < Config.captionHeightJp; ++yOff)
                        for (int x = 0; x < Config.ANIMATION_WIDTH; ++x)
                        {
                            if (bitsRemaining == 0)
                            {
                                b = cachedCaption[i++];
                                bitsRemaining = 8;
                            }

                            drawBuffer[y + yOff, x] = (b & 1) == 1 ? fgColor : Config.captionBgColor;
                            b >>= 1;
                            --bitsRemaining;
                        }
                }
            }
            else
            {
                int captionIndex;
                for (captionIndex = 0; captionIndex < Config.captionTimesRow.Length; ++captionIndex)
                {
                    if (framesDecoded < Config.captionTimesRow[captionIndex]) break;
                }
                if (captionIndex >= Config.captionTimesRow.Length)
                    captionIndex = Config.captionTimesRow.Length - 1;
                int captionTime = Config.captionTimesRow[captionIndex];

                // Caching is not in the firmware because if can access flash directly as memory
                if (cachedLang != mainController.currentLanguage || cachedCaptionIndex != captionIndex || cachedCaption == null)
                {
                    cachedLang = mainController.currentLanguage;
                    cachedCaptionIndex = captionIndex;
                    cachedCaption = resourceManager.GetRawCaption(cachedLang, captionIndex);
                }

                // Always clear the top part of the caption area (used for JP ruby)
                int endYTop = y + Config.captionHeightGeneral - Config.captionHeightRow;
                for (; y < endYTop; ++y)
                    for (int x = 0; x < Config.ANIMATION_WIDTH; ++x)
                    {
                        drawBuffer[y, x] = Config.captionBgColor;
                    }

                if (captionTime - framesDecoded <= 2)
                {
                    // Clear caption area for the last two frames of current caption
                    int endY = y + Config.captionHeightRow;
                    for (; y < endY; ++y)
                        for (int x = 0; x < Config.ANIMATION_WIDTH; ++x)
                        {
                            drawBuffer[y, x] = Config.captionBgColor;
                        }
                }
                else
                {
                    byte b = 0;
                    byte bitsRemaining = 0;
                    int i = 0;
                    var fgColor = captionIndex == Config.captionTimesRow.Length - 1 ? Config.captionLastColor : Config.captionFgColor;
                    for (int yOff = 0; yOff < Config.captionHeightRow; ++yOff)
                        for (int x = 0; x < Config.ANIMATION_WIDTH; ++x)
                        {
                            if (bitsRemaining == 0)
                            {
                                b = cachedCaption[i++];
                                bitsRemaining = 8;
                            }

                            drawBuffer[y + yOff, x] = (b & 1) == 1 ? fgColor : Config.captionBgColor;
                            b >>= 1;
                            --bitsRemaining;
                        }
                }
            }
        }
    }
}
