using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Memories.GifDisplay
{
    [RequireComponent(typeof(RawImage), typeof(AspectRatioFitter))]
    public class GIFAnimator : MonoBehaviour
    {
        private RawImage gifDisplay;
        private AspectRatioFitter aspectRatioFitter;
        [SerializeField, HideInInspector]
        private GIFImage gifImage;
        private int frameIndex;
        private bool paused;
        void Awake()
        {
            gifDisplay = GetComponent<RawImage>();
            aspectRatioFitter = GetComponent<AspectRatioFitter>();
        }

        private void OnEnable()
        {
            Start();
        }

        void Start()
        {
            if (gifImage != null)
                AnimateGIF(gifImage);
        }

        public void AnimateGIF(GIFImage gifImage)
        {
            frameIndex = 0;
            this.gifImage = gifImage;
            if (gifImage.Frames.Count > 0)
            {
                gifDisplay.texture = gifImage.Frames[frameIndex].m_texture2d;
                aspectRatioFitter.aspectRatio = (float)gifDisplay.texture.width / gifDisplay.texture.height;
                StartCoroutine(StartAnimation());
            }
        }

        private IEnumerator StartAnimation()
        {
            while (!paused)
            {
                yield return new WaitForSeconds(gifImage.Frames[frameIndex].m_delaySec);
                if (frameIndex < gifImage.Frames.Count - 1)
                    frameIndex++;
                else
                    frameIndex = 0;
                gifDisplay.texture = gifImage.Frames[frameIndex].m_texture2d;
            }
        }

        public void Resume()
        {
            paused = false;
            StartCoroutine(StartAnimation());
        }

        public void Pause()
        {
            paused = true;
        }
    }
}
