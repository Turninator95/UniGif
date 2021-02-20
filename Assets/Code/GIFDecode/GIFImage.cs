using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Memories.GifDisplay
{
    [Serializable]
    public class GIFImage : MonoBehaviour
    {
        #region Fields
        // Decoded GIF texture list
        [SerializeField, HideInInspector]
        private List<GifTexture> frames;
        [SerializeField, HideInInspector]
        private int width, height, fps;
        [SerializeField, HideInInspector]
        private string filePath;
        private GifDecoderJob gifDecoderJob;
        #endregion

        #region Properties
        public List<GifTexture> Frames { get => frames; }
        public int Width { get => width; }
        public int Height { get => height; }
        public int Fps { get => fps; }
        public string FilePath { get => filePath; }
        #endregion

        public GIFImage()
        {
            frames = new List<GifTexture>();
            gifDecoderJob = new GifDecoderJob();
        }

        /// <summary>
        /// Loads the file from the specified path and extracts the frames
        /// </summary>
        /// <param name="file">Path to GIF file</param>
        public IEnumerator Load(string file, Action<GIFImage> callback)
        {
            filePath = file;
            byte[] gifBytes = File.ReadAllBytes(file);
            gifDecoderJob.gifBytes = gifBytes;
            gifDecoderJob.Start();
            yield return StartCoroutine(gifDecoderJob.WaitFor());
            frames = gifDecoderJob.gifTexList;
            callback.Invoke(this);
        }
    }
}