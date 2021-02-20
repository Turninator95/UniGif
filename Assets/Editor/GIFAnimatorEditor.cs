using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Memories.GifDisplay;

[CustomEditor(typeof(GIFAnimator))]
public class GIFAnimatorEditor : Editor
{
    private Texture gifFile;
    private bool updatedReference = false;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Texture newGifFile = (Texture)EditorGUILayout.ObjectField("GIF File", gifFile, typeof(Texture), false);
        if (newGifFile != gifFile)
            updatedReference = true;
        if (newGifFile != null && updatedReference)
        {
            updatedReference = false;
            string assetPath = AssetDatabase.GetAssetPath(newGifFile);

            if (assetPath.EndsWith(".gif"))
                gifFile = newGifFile;
            else
                Debug.LogError($"The provided file is not a GIF.");
        }
    }
}