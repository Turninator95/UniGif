using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Memories
{
    [System.Serializable]
    public class Sharable : MonoBehaviour
    {
        [SerializeField]
        private string filePath;
        public string FilePath { get => filePath; set => filePath = value; }
    }
}