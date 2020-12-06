using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Framebuffer : MonoBehaviour
{
    Texture2D texture;
    ushort[] framebuffer;
    byte[] frameBufferSubmit;

    public SpriteRenderer spriteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        texture = new Texture2D(Config.FRAMEBUFFER_WIDTH, Config.FRAMEBUFFER_HEIGHT, TextureFormat.RGB565, false);
        texture.filterMode = FilterMode.Point;
        framebuffer = new ushort[Config.FRAMEBUFFER_WIDTH * Config.FRAMEBUFFER_HEIGHT];
        frameBufferSubmit = new byte[framebuffer.Length * 2];
        var sprite = Sprite.Create(texture, new Rect(0, 0, Config.FRAMEBUFFER_WIDTH, Config.FRAMEBUFFER_HEIGHT),
            new Vector2(0.5f, 0.5f), 100);
        spriteRenderer.sprite = sprite;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void FrameUpdate()
    {
        Buffer.BlockCopy(framebuffer, 0, frameBufferSubmit, 0, frameBufferSubmit.Length);
        texture.LoadRawTextureData(frameBufferSubmit);
        texture.Apply();
        // Clear framebuffer after uploading
        for (int i = 0; i < framebuffer.Length; ++i)
            framebuffer[i] = 0;
    }

    public void SetPixel(int x, int y, ushort color)
    {
        y = Config.FRAMEBUFFER_HEIGHT - 1 - y;
        framebuffer[y * Config.FRAMEBUFFER_WIDTH + x] = color;
    }

    public ushort GetPixel(int x, int y)
    {
        y = Config.FRAMEBUFFER_HEIGHT - 1 - y;
        return framebuffer[y * Config.FRAMEBUFFER_WIDTH + x];
    }
}
