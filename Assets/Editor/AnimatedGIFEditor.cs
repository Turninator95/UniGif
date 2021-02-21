using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AnimatedGIF))]
public class AnimatedGIFEditor : Editor
{
    private AnimatedGIF animatedGIF;
    public override void OnInspectorGUI()
    {
        animatedGIF = (AnimatedGIF)target;
        GUILayout.Label("General Information", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Dimensions", GUILayout.Width(100));
        GUILayout.TextField($"{animatedGIF.Width}x{animatedGIF.Height}");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Frame Count", GUILayout.Width(100));
        GUILayout.TextField($"{animatedGIF.Frames.Count}");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("FPS", GUILayout.Width(100));
        GUILayout.TextField($"{animatedGIF.Fps}");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Source File Path", GUILayout.Width(100));
        GUILayout.TextArea($"{animatedGIF.FilePath}");
        GUILayout.EndHorizontal();
        GUILayout.Label("GIF Frames", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        for (int i = 0; i < animatedGIF.Frames.Count; i++)
        {
            EditorGUILayout.ObjectField($"GIF Frame {i}", animatedGIF.Frames[i].m_texture2d, typeof(Texture2D), false);
        }
    }
}
