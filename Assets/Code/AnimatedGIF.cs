using Memories.GifDisplay;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class AnimatedGIF : ScriptableObject
{
    #region Fields
    // Decoded GIF texture list
    [SerializeField]
    private List<GifTexture> frames;
    [SerializeField]
    private int width, height, fps;
    [SerializeField]
    private string filePath;
    private GifDecoderJob gifDecoderJob;
    #endregion

    #region Properties
    public List<GifTexture> Frames { get => frames; }
    public int Width { get => width; }
    public int Height { get => height; }
    public int Fps { get => fps; }
    public string FilePath { get => filePath; }
    public GifDecoderJob GifDecoderJob { get => gifDecoderJob; }
    #endregion

    public AnimatedGIF()
    {
        frames = new List<GifTexture>();
        gifDecoderJob = new GifDecoderJob();
    }

    /// <summary>
    /// Loads the file from the specified path and extracts the frames
    /// </summary>
    /// <param name="assetPath">Path to GIF file</param>
    public void Load(string assetPath)
    {
        filePath = $"{Application.dataPath}{assetPath.Remove(0, assetPath.IndexOf('/'))}";
        byte[] gifBytes = File.ReadAllBytes(filePath);
        gifDecoderJob.gifBytes = gifBytes;
        gifDecoderJob.DecodingCompleted += LoadingCompleted;
        gifDecoderJob.Start();
    }

    public void LoadingCompleted(List<GifTexture> gifTextures)
    {
        GifTexture[] textures = new GifTexture[gifTextures.Count];
        gifTextures.CopyTo(textures);
        frames = textures.ToList();
        if (frames.Count > 0)
        {
            width = frames[0].m_texture2d.width;
            height = frames[0].m_texture2d.height;
            fps = Mathf.RoundToInt(1 / frames[0].m_delaySec);
        }
        
    }
}
