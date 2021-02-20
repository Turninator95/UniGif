using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Memories
{
    public class FocusView : MonoBehaviour
    {
        [SerializeField]
        private Gallery gallery;
        [SerializeField]
        private GameObject shareButton;

        private void Awake()
        {
#if UNITY_ANDROID || UNITY_IOS
            shareButton.SetActive(true);
#else
            shareButton.SetActive(false);
#endif
        }

        public void AddFocusObject(GameObject gameObject)
        {
            GameObject focusObject = Instantiate(gameObject, transform);
            focusObject.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            this.gameObject.SetActive(true);
        }

        public void Close()
        {
            Destroy(transform.GetChild(transform.childCount - 1).gameObject);
            gameObject.SetActive(false);
            gallery.gameObject.SetActive(true);
        }

        public void Share()
        {
           //sharing
        }
    }
}

