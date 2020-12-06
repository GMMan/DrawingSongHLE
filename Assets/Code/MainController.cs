using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class MainController : MonoBehaviour
{
    bool isMenu;
    bool loaded;
    Language cachedLanguage;
    byte[] cachedBanner;
    int menuTick;
    float frameTimer;
    float timePerFrame;

    public ResourceManager resourceManager;
    public AudioController audioController;
    public AnimationDecoder animationDecoder;
    public Framebuffer framebuffer;

    public bool isPlaying { get; private set; }
    public Language currentLanguage { get; private set; }
    public int mainTick { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        timePerFrame = 1 / 60f;
        ResetAll();
    }

    // Update is called once per frame
    void Update()
    {
        CheckAndLoadResources();
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentLanguage == 0)
                currentLanguage = (Language)((int)Language.Max - 1);
            else
                currentLanguage -= 1;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentLanguage == Language.Max - 1)
                currentLanguage = 0;
            else
                currentLanguage += 1;
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (isMenu)
            {
                isMenu = false;
                isPlaying = true;
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isMenu) isPlaying = !isPlaying;
        }
        if (Input.GetKeyDown(KeyCode.F5))
        {
            ResetAll();
        }

        frameTimer += Time.deltaTime;
        //Debug.Log($"Time: dt = {Time.deltaTime}, ideal time per frame = {timePerFrame}");
        bool shouldFlip = false;
        while (frameTimer >= timePerFrame)
        {
            frameTimer -= timePerFrame;
            Profiler.BeginSample("Main frame update");
            animationDecoder.decodingActive = true;
            FrameUpdate();
            Profiler.EndSample();
            shouldFlip = true;
        }
        if (shouldFlip) framebuffer.FrameUpdate();
    }

    private void FrameUpdate()
    {
        if (!loaded) return;

        if (animationDecoder.framesDecoded == 0 || isPlaying && !isMenu)
        {
            if (animationDecoder.decodingActive)
            {
                Profiler.BeginSample("Animation decoder update");
                animationDecoder.AnimUpdate();
                Profiler.EndSample();
            }
        }

        if (animationDecoder.framesDecoded == 0) return;

        // Render to framebuffer
        // First, create a buffer for the frame surrounding the animation area
        const int SIDEBUF_WIDTH = 6;
        const int SIDEBUF_HEIGHT = 6;
        byte[] sideBuf = new byte[SIDEBUF_WIDTH * SIDEBUF_HEIGHT];
        for (int y = 0; y < SIDEBUF_HEIGHT; ++y)
            for (int x = 0; x < SIDEBUF_WIDTH; ++x)
            {
                sideBuf[(y + 2) % SIDEBUF_HEIGHT * SIDEBUF_WIDTH + ((x + 2) % SIDEBUF_WIDTH)] = animationDecoder.indexedPixels[y, x];
            }

        // Draw left and right frame and main anim area
        for (int y = Config.animYPos; y < Config.animYPos + Config.ANIMATION_HEIGHT; ++y)
        {
            for (int x = 0; x < Config.animXPos; ++x)
            {
                framebuffer.SetPixel(x, y, Config.palette[sideBuf[y % SIDEBUF_HEIGHT * SIDEBUF_WIDTH + (x % SIDEBUF_WIDTH)]]);
            }
            for (int x = 0; x < Config.ANIMATION_WIDTH; ++x)
            {
                framebuffer.SetPixel(Config.animXPos + x, y, Config.palette[animationDecoder.indexedPixels[y - Config.animYPos, x]]);
            }
            for (int x = Config.animXPos + Config.ANIMATION_WIDTH; x < Config.FRAMEBUFFER_WIDTH; ++x)
            {
                framebuffer.SetPixel(x, y, Config.palette[sideBuf[y % SIDEBUF_HEIGHT * SIDEBUF_WIDTH + (x % SIDEBUF_WIDTH)]]);
            }
        }

        // Draw top frame
        for (int y = 0; y < Config.animYPos; ++y)
            for (int x = 0; x < Config.FRAMEBUFFER_WIDTH; ++x)
            {
                framebuffer.SetPixel(x, y, Config.palette[sideBuf[y % SIDEBUF_HEIGHT * SIDEBUF_WIDTH + (x % SIDEBUF_WIDTH)]]);
            }

        // Clear bottom with black
        for (int y = Config.animYPos + Config.FRAMEBUFFER_HEIGHT; y < Config.FRAMEBUFFER_HEIGHT; ++y)
            for (int x = 0; x < Config.FRAMEBUFFER_WIDTH; ++x)
            {
                framebuffer.SetPixel(x, y, 0x0000);
            }

        if (cachedLanguage != currentLanguage || cachedBanner == null)
        {
            cachedLanguage = currentLanguage;
            cachedBanner = resourceManager.GetLanguageBanner(currentLanguage);
        }

        if (isMenu || animationDecoder.framesDecoded < 8)
        {
            // Draw banner
            int bannerYAdjust = Config.langBannerHeightAdjustments[(int)currentLanguage];

            byte b = 0;
            byte bitsRemaining = 0;
            int i = 0;
            for (int y = bannerYAdjust; y < Config.langBannerHeight - 2 * bannerYAdjust; ++y)
                for (int x = 0; x < Config.ANIMATION_WIDTH; ++x)
                {
                    if (bitsRemaining == 0)
                    {
                        b = cachedBanner[i++];
                        bitsRemaining = 8;
                    }

                    framebuffer.SetPixel(x + Config.animXPos, y + Config.langBannerYOffset, (b & 1) == 1 ? Config.langBannerFgColor : Config.langBannerBgColor);
                    b >>= 1;
                    --bitsRemaining;
                }
        }

        if (isMenu)
        {
            if (menuTick % 0x40 < 0x30)
            {
                // Draw arrows and frame
                DrawHorizontalTriangle(8, 60, 20, 18, Config.langBannerFgColor);
                DrawHorizontalTriangle(312, 60, -20, 18, Config.langBannerFgColor);
                DrawRectFrame(32, 27, 288, 93, Config.langBannerFgColor);
            }

            ++menuTick;
        }
    }

    void DrawHorizontalTriangle(int x, int y, int width, int height, ushort color)
    {
        if (width != 0 && height != 0)
        {
            int direction = width < 1 ? -1 : 1;
            width *= direction;
            int maxDist = 1;
            int i = 0;
            for (int xOff = 0; xOff < width; ++xOff)
            {
                for (int currDist = 0; currDist < maxDist; ++currDist)
                {
                    framebuffer.SetPixel(x + xOff * direction, y + currDist, color);
                    if (currDist != 0)
                    {
                        framebuffer.SetPixel(x + xOff * direction, y - currDist, color);
                    }
                }

                i += height;
                if (i >= width)
                {
                    ++maxDist;
                    i -= width;
                }
            }
        }
    }

    void DrawRectFrame(int left, int top, int right, int bottom, ushort color)
    {
        for (int x = left; x <= right; ++x)
        {
            framebuffer.SetPixel(x, top, color);
            framebuffer.SetPixel(x, bottom, color);
        }
        // Note: original condition was top..bottom, but since we covered it with above,
        // we can skip two rows
        for (int y = top + 1; y < bottom; ++y)
        {
            framebuffer.SetPixel(left, y, color);
            framebuffer.SetPixel(right, y, color);
        }
    }

    void CheckAndLoadResources()
    {
        if (!loaded && resourceManager.isReady)
        {
            audioController.LoadResources();
            animationDecoder.LoadResources();
            loaded = true;
        }
    }

    void ResetAll()
    {
        audioController.ResetController();
        animationDecoder.ResetController();
        loaded = false;
        isPlaying = false;
        isMenu = true;
        menuTick = 0;
        cachedBanner = null;
    }
}
