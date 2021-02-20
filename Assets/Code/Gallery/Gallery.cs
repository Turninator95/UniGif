using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Memories.GifDisplay;


namespace Memories
{
    public class Gallery : MonoBehaviour
    {
        [SerializeField]
        private string saveFolder;
        private GraphicRaycaster graphicRaycaster;
        [SerializeField]
        private RectTransform scrollView;
        [SerializeField]
        private FocusView focusView;
        [SerializeField]
        private GameObject menuButton;

        void OnEnable()
        {
            menuButton.SetActive(true);
        }

        // Start is called before the first frame update
        void Start()
        {
            graphicRaycaster = FindObjectOfType<GraphicRaycaster>();
#if UNITY_EDITOR || UNITY_STANDALONE
            saveFolder = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyPictures, Environment.SpecialFolderOption.DoNotVerify)}" +
                   $"/{Application.productName}";

            if (!Directory.Exists(saveFolder))
                Directory.CreateDirectory(saveFolder);
#endif
#if UNITY_ANDROID || UNITY_IOS
            saveFolder = Application.persistentDataPath;
#endif

            foreach (string file in Directory.GetFiles(saveFolder))
            {
                if (file.EndsWith(".jpg"))
                    CreateScreenshotContainer(file);
                else if (file.EndsWith(".gif"))
                    CreateGifContainer(file);
            }

            GridLayoutGroup gridLayoutGroup = scrollView.GetComponent<GridLayoutGroup>();
            scrollView.sizeDelta = new Vector2(scrollView.sizeDelta.x, (gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y) * Mathf.CeilToInt((float)scrollView.childCount / 3));
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.touchCount == 1 || Input.GetMouseButtonDown(0))
                CheckTouchPosition();
        }

        /// <summary>
        /// Checks whether a valid gallery object has been hit and puts that into the focus view
        /// </summary>
        private void CheckTouchPosition()
        {
            Vector2 inputPosition;
            if (Input.touchCount == 1)
                inputPosition = Input.touches[0].position;
            else
                inputPosition = Input.mousePosition;

            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = inputPosition;
            List<RaycastResult> results = new List<RaycastResult>();
            graphicRaycaster.Raycast(pointerEventData, results);

            if (results.Count > 0)
            {
                foreach (RaycastResult raycastResult in results)
                {
                    if (raycastResult.gameObject.name == "ScreenshotContainer" || raycastResult.gameObject.name == "GifContainer")
                    {
                        menuButton.SetActive(false);
                        scrollView.gameObject.SetActive(false);
                        focusView.AddFocusObject(raycastResult.gameObject);
                        break;
                    }
                }
            }
        }

        private void CreateScreenshotContainer(string file)
        {
            Texture2D screenShot = new Texture2D(8, 8);
            byte[] textureData = File.ReadAllBytes(file);
            screenShot.LoadImage(textureData);

            GameObject screenshotContainer = new GameObject("ScreenshotContainer");
            screenshotContainer.AddComponent<Sharable>().FilePath = file;
            screenshotContainer.layer = LayerMask.NameToLayer("UI");
            screenshotContainer.transform.parent = scrollView;
            screenshotContainer.AddComponent<RawImage>().texture = screenShot;
            AspectRatioFitter aspectRatioFitter = screenshotContainer.AddComponent<AspectRatioFitter>();
            aspectRatioFitter.aspectRatio = (float)screenShot.width / screenShot.height;
        }
        private void CreateGifContainer(string file)
        {
            GameObject gifContainer = new GameObject("GifContainer");
            gifContainer.AddComponent<Sharable>().FilePath = file;
            gifContainer.transform.parent = scrollView;
            GIFImage gifImage = gifContainer.AddComponent<GIFImage>();
            StartCoroutine(gifImage.Load(file, GifLoaded));
        }
        private void GifLoaded(GIFImage loadedImage)
        {
            loadedImage.gameObject.AddComponent<GIFAnimator>().AnimateGIF(loadedImage);
        }
    }
}