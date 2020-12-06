using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Config
{
    public const int ANIMATION_WIDTH = 256;
    public const int ANIMATION_HEIGHT = 192;
    public const int FRAMEBUFFER_WIDTH = 320;
    public const int FRAMEBUFFER_HEIGHT = 240;

    public static int animXPos = 32;
    public static int animYPos = 32;

    // RGB565 palette
    public static ushort[] palette = new ushort[] { 0x0000, 0xffff, 0xf800, 0x001f };
    public static int numFrames = 356; // 12 FPS
    public static string decryptedExtFlashName = "external.bin";
    public static int animationOffset = 0x12d44;
    public static int animationLength = 0x1CD06;
    public static int audioOffset = 0x2fa64;
    public static int audioLength = 0x5a240;
    public static int captionHeightGeneral = 27;
    public static int captionHeightRow = 23;
    public static int captionHeightJp = 27;
    // The following colors are indexes into palette
    public static byte captionBgColor = 1;
    public static byte captionFgColor = 0;
    public static byte captionLastColor = 2;
    public static int[] captionOffsetsRow = new int[]
    {
        0x95724,
        0x95a04,
        0x95ce4,
        0x95fc4,
        0x962a4,
        0x96584,
        0x96584,
        0x96864,
        0x96b44,
        0x96e24,
        0x97104,
        0x90984,
        0x90c64,
        0x90f44,
        0x91224,
        0x91504,
        0x917e4,
        0x917e4,
        0x91ac4,
        0x91da4,
        0x92084,
        0x97104,
        0x8ecc4,
        0x8efa4,
        0x8f284,
        0x8f564,
        0x8f844,
        0x8fb24,
        0x8fe04,
        0x900e4,
        0x903c4,
        0x906a4,
        0x97104,
        0x92364,
        0x92644,
        0x92924,
        0x92c04,
        0x92ee4,
        0x931c4,
        0x931c4,
        0x934a4,
        0x93784,
        0x93a64,
        0x97104,
        0x8d2e4,
        0x8d5c4,
        0x8d8a4,
        0x8db84,
        0x8de64,
        0x8e144,
        0x8e144,
        0x8e424,
        0x8e704,
        0x8e9e4,
        0x97104,
        0x93d44,
        0x94024,
        0x94304,
        0x945e4,
        0x948c4,
        0x94ba4,
        0x94ba4,
        0x94e84,
        0x95164,
        0x95444,
        0x97104,
    };
    public static int[] captionOffsetsJp = new int[]
    {
        0x973e4,
        0x97744,
        0x97aa4,
        0x97e04,
        0x97e04,
        0x98164,
        0x984c4,
        0x98824,
    };
    public static int[] captionTimesRow = new int[]
    {
        0x30, 0x48, 0x75, 0x8C,
        0xAA, 0xC2, 0xD5, 0xEF,
        0x107, 0x13D, 0x3E7,
    };
    public static int[] captionTimesJp = new int[]
    {
        0x48, 0x75, 0xAB, 0xC2,
        0xD5, 0x107, 0x13D, 0x3E7,
    };
    public static int langBannerYOffset = 29;
    // The following colors are RGB565
    public static ushort langBannerFgColor = 0xffff;
    public static ushort langBannerBgColor = 0xf800;
    public static int langBannerHeight = 62;
    public static int[] langBannerOffsets = new int[]
    {
        0x8cb24,
        0x8a464,
        0x89ca4,
        0x8ac24,
        0x8b3e4,
        0x8c364,
        0x8bba4,
    };
    public static int[] langBannerHeightAdjustments = new int[]
    {
        0, 3, 0, 3, 2, 2, 2
    };
}
