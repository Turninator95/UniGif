using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace Memories.GifDisplay
{
    [RequireComponent(typeof(RawImage), typeof(AspectRatioFitter)), ExecuteInEditMode]
    public class GIFAnimator : MonoBehaviour
    {
        private RawImage gifDisplay;
        private AspectRatioFitter aspectRatioFitter;
        [SerializeField]
        private AnimatedGIF animatedGIF;
        private int frameIndex;
        [SerializeField]
        private bool animate = false;
        private bool paused = false;
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
            if (animatedGIF != null)
            {
                AnimateGIF(animatedGIF);
            }
        }

        public void AnimateGIF(AnimatedGIF animatedGIF)
        {
            frameIndex = 0;
            this.animatedGIF = animatedGIF;
            if (animatedGIF.Frames.Count > 0)
            {
                gifDisplay.texture = animatedGIF.Frames[frameIndex].m_texture2d;
                aspectRatioFitter.aspectRatio = (float)gifDisplay.texture.width / gifDisplay.texture.height;
#if UNITY_EDITOR
                EditorCoroutineUtility.StartCoroutine(StartAnimation(), this);
#endif
#if UNITY_STANDALONE
                StartCoroutine(StartAnimation());
#endif
            }
        }

        private IEnumerator StartAnimation()
        {
            while (!paused)
            {
                yield return new WaitForSeconds(animatedGIF.Frames[frameIndex].m_delaySec);
                if (frameIndex < animatedGIF.Frames.Count - 1)
                    frameIndex++;
                else
                    frameIndex = 0;
                gifDisplay.texture = animatedGIF.Frames[frameIndex].m_texture2d;
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
