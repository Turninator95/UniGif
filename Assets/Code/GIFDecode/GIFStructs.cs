﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace Memories.GifDisplay
{
    /// <summary>
    /// Gif Texture
    /// </summary>
    [Serializable]
    public class GifTexture
    {
        // Texture
        [SerializeField, HideInInspector]
        public Texture2D m_texture2d;
        // Delay time until the next texture.
        [SerializeField, HideInInspector]
        public float m_delaySec;

        public GifTexture(Texture2D texture2d, float delaySec)
        {
            m_texture2d = texture2d;
            m_delaySec = delaySec;
        }
    }

    public class DecodedFrame
    {
        private byte[] gifData;
        private List<byte[]> colorTable;
        private BackgroundColor backgroundColor;
        private float frameDelay;
        private ushort disposalMethod;
        private int transparentIndex;
        private ImageBlock imageBlock;

        public DecodedFrame(byte[] gifData, BackgroundColor backgroundColor, float frameDelay, ushort disposalMethod, List<byte[]> colorTable, int transparentIndex, ImageBlock imageBlock)
        {
            this.gifData = gifData;
            this.backgroundColor = backgroundColor;
            this.frameDelay = frameDelay;
            this.disposalMethod = disposalMethod;
            this.colorTable = colorTable;
            this.transparentIndex = transparentIndex;
            this.imageBlock = imageBlock;
        }

        public byte[] GifData { get => gifData; }
        public BackgroundColor BackgroundColor { get => backgroundColor; }
        public float FrameDelay { get => frameDelay; }
        public ushort DisposalMethod { get => disposalMethod; }
        public List<byte[]> ColorTable { get => colorTable; }
        public int TransparentIndex { get => transparentIndex; }
        public ImageBlock ImageBlock { get => imageBlock; }
    }

    public struct BackgroundColor
    {
        public byte r, g, b, a;
    }

    /// <summary>
    /// GIF Data Format
    /// </summary>
    public struct GifData
    {
        // Signature
        public byte m_sig0, m_sig1, m_sig2;
        // Version
        public byte m_ver0, m_ver1, m_ver2;
        // Logical Screen Width
        public ushort m_logicalScreenWidth;
        // Logical Screen Height
        public ushort m_logicalScreenHeight;
        // Global Color Table Flag
        public bool m_globalColorTableFlag;
        // Color Resolution
        public int m_colorResolution;
        // Sort Flag
        public bool m_sortFlag;
        // Size of Global Color Table
        public int m_sizeOfGlobalColorTable;
        // Background Color Index
        public byte m_bgColorIndex;
        // Pixel Aspect Ratio
        public byte m_pixelAspectRatio;
        // Global Color Table
        public List<byte[]> m_globalColorTable;
        // ImageBlock
        public List<ImageBlock> m_imageBlockList;
        // GraphicControlExtension
        public List<GraphicControlExtension> m_graphicCtrlExList;
        // Comment Extension
        public List<CommentExtension> m_commentExList;
        // Plain Text Extension
        public List<PlainTextExtension> m_plainTextExList;
        // Application Extension
        public ApplicationExtension m_appEx;
        // Trailer
        public byte m_trailer;

        public string signature
        {
            get
            {
                char[] c = { (char)m_sig0, (char)m_sig1, (char)m_sig2 };
                return new string(c);
            }
        }

        public string version
        {
            get
            {
                char[] c = { (char)m_ver0, (char)m_ver1, (char)m_ver2 };
                return new string(c);
            }
        }

        public void Dump(List<string> debugMessages)
        {
            //ADD Stuff
            debugMessages.Add($"{DateTime.Now} [DEBUG]: GIF Type: {signature} - {version}");
            debugMessages.Add($"{DateTime.Now} [DEBUG]: Image Size: {m_logicalScreenWidth} x {m_logicalScreenHeight}");
            debugMessages.Add($"{DateTime.Now} [DEBUG]: Animation Image Count: {m_imageBlockList.Count}");
            debugMessages.Add($"{DateTime.Now} [DEBUG]: Animation Loop Count (0 is infinite): {m_appEx.loopCount}");
            if (m_graphicCtrlExList != null && m_graphicCtrlExList.Count > 0)
            {
                var sb = new StringBuilder("Animation Delay Time (1/100sec)");
                for (int i = 0; i < m_graphicCtrlExList.Count; i++)
                {
                    sb.Append(", ");
                    sb.Append(m_graphicCtrlExList[i].m_delayTime);
                }
                debugMessages.Add($"{DateTime.Now} [DEBUG]: {sb.ToString()}");
            }
            debugMessages.Add($"{DateTime.Now} [DEBUG]: Application Identifier: {m_appEx.applicationIdentifier}");
            debugMessages.Add($"{DateTime.Now} [DEBUG]: Application Authentication Code: {m_appEx.applicationAuthenticationCode}");
        }
    }

    /// <summary>
    /// Image Block
    /// </summary>
    public struct ImageBlock
    {
        // Image Separator
        public byte m_imageSeparator;
        // Image Left Position
        public ushort m_imageLeftPosition;
        // Image Top Position
        public ushort m_imageTopPosition;
        // Image Width
        public ushort m_imageWidth;
        // Image Height
        public ushort m_imageHeight;
        // Local Color Table Flag
        public bool m_localColorTableFlag;
        // Interlace Flag
        public bool m_interlaceFlag;
        // Sort Flag
        public bool m_sortFlag;
        // Size of Local Color Table
        public int m_sizeOfLocalColorTable;
        // Local Color Table
        public List<byte[]> m_localColorTable;
        // LZW Minimum Code Size
        public byte m_lzwMinimumCodeSize;
        // Block Size & Image Data List
        public List<ImageDataBlock> m_imageDataList;

        public struct ImageDataBlock
        {
            // Block Size
            public byte m_blockSize;
            // Image Data
            public byte[] m_imageData;
        }
    }

    /// <summary>
    /// Graphic Control Extension
    /// </summary>
    public struct GraphicControlExtension
    {
        // Extension Introducer
        public byte m_extensionIntroducer;
        // Graphic Control Label
        public byte m_graphicControlLabel;
        // Block Size
        public byte m_blockSize;
        // Disposal Mothod
        public ushort m_disposalMethod;
        // Transparent Color Flag
        public bool m_transparentColorFlag;
        // Delay Time
        public ushort m_delayTime;
        // Transparent Color Index
        public byte m_transparentColorIndex;
        // Block Terminator
        public byte m_blockTerminator;
    }

    /// <summary>
    /// Comment Extension
    /// </summary>
    public struct CommentExtension
    {
        // Extension Introducer
        public byte m_extensionIntroducer;
        // Comment Label
        public byte m_commentLabel;
        // Block Size & Comment Data List
        public List<CommentDataBlock> m_commentDataList;

        public struct CommentDataBlock
        {
            // Block Size
            public byte m_blockSize;
            // Image Data
            public byte[] m_commentData;
        }
    }

    /// <summary>
    /// Plain Text Extension
    /// </summary>
    public struct PlainTextExtension
    {
        // Extension Introducer
        public byte m_extensionIntroducer;
        // Plain Text Label
        public byte m_plainTextLabel;
        // Block Size
        public byte m_blockSize;
        // Block Size & Plain Text Data List
        public List<PlainTextDataBlock> m_plainTextDataList;

        public struct PlainTextDataBlock
        {
            // Block Size
            public byte m_blockSize;
            // Plain Text Data
            public byte[] m_plainTextData;
        }
    }

    /// <summary>
    /// Application Extension
    /// </summary>
    public struct ApplicationExtension
    {
        // Extension Introducer
        public byte m_extensionIntroducer;
        // Extension Label
        public byte m_extensionLabel;
        // Block Size
        public byte m_blockSize;
        // Application Identifier
        public byte m_appId1, m_appId2, m_appId3, m_appId4, m_appId5, m_appId6, m_appId7, m_appId8;
        // Application Authentication Code
        public byte m_appAuthCode1, m_appAuthCode2, m_appAuthCode3;
        // Block Size & Application Data List
        public List<ApplicationDataBlock> m_appDataList;

        public struct ApplicationDataBlock
        {
            // Block Size
            public byte m_blockSize;
            // Application Data
            public byte[] m_applicationData;
        }

        public string applicationIdentifier
        {
            get
            {
                char[] c = { (char)m_appId1, (char)m_appId2, (char)m_appId3, (char)m_appId4, (char)m_appId5, (char)m_appId6, (char)m_appId7, (char)m_appId8 };
                return new string(c);
            }
        }

        public string applicationAuthenticationCode
        {
            get
            {
                char[] c = { (char)m_appAuthCode1, (char)m_appAuthCode2, (char)m_appAuthCode3 };
                return new string(c);
            }
        }

        public int loopCount
        {
            get
            {
                if (m_appDataList == null || m_appDataList.Count < 1 ||
                    m_appDataList[0].m_applicationData.Length < 3 ||
                    m_appDataList[0].m_applicationData[0] != 0x01)
                {
                    return 0;
                }
                return BitConverter.ToUInt16(m_appDataList[0].m_applicationData, 1);
            }
        }
    }
}
