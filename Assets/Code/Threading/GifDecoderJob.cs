using Memories.GifDisplay;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class GifDecoderJob : ThreadedJob
{
    private static List<string> debugMessages = new List<string>();
    public static List<string> DebugMessages { get => debugMessages; }

    public Action<List<GifTexture>> DecodingCompleted;

    public byte[] gifBytes;  // job data has to be set externally
    private List<DecodedFrame> decodedFrames = new List<DecodedFrame>();
    private List<ushort> disposalMethodList = new List<ushort>();
    public List<GifTexture> gifTexList = new List<GifTexture>();
    private List<Color32[]> rawTextures = new List<Color32[]>();

    private int width = 0, height = 0;

    protected override void ThreadFunction()
    {
        var gifData = new GifData();
        if (SetGifData(gifBytes, ref gifData, true) == false)
            debugMessages.Add($"{DateTime.Now} [ERROR]: GifData could not be set.");

        if (gifData.m_imageBlockList == null || gifData.m_imageBlockList.Count < 1)
            return;

        width = gifData.m_logicalScreenWidth;
        height = gifData.m_logicalScreenHeight;

        int imgIndex = 0;

        for (int i = 0; i < gifData.m_imageBlockList.Count; i++)
        {
            int dataIndex = 0;

            byte[] decodedData = GetDecodedData(gifData.m_imageBlockList[i]);

            GraphicControlExtension? graphicCtrlEx = GetGraphicCtrlExt(gifData, imgIndex);

            int transparentIndex = GetTransparentIndex(graphicCtrlEx);

            disposalMethodList.Add(GetDisposalMethod(graphicCtrlEx));

            BackgroundColor bgColor;
            List<byte[]> colorTable = GetColorTableAndSetBgColor(gifData, gifData.m_imageBlockList[i], transparentIndex, out bgColor);

            float delaySec = GetDelaySec(graphicCtrlEx);
            ushort disposalMethod;
            if (imgIndex > 0)
            {
                disposalMethod = disposalMethodList[imgIndex - 1];
            }

            else
                disposalMethod = (ushort)2;
            DecodedFrame decodedFrame = new DecodedFrame(decodedData, bgColor, delaySec, disposalMethod, colorTable, transparentIndex, gifData.m_imageBlockList[i]);
            rawTextures.Add(GetTextureData(decodedFrame, ref dataIndex, false));
            decodedFrames.Add(decodedFrame);
            imgIndex++;
        }
    }
    protected override void OnFinished()
    {
        for (int i = 0; i < rawTextures.Count; i++)
        {
            Texture2D gifFrame = new Texture2D(width, height, TextureFormat.ARGB32, false, false);
            gifFrame.filterMode = FilterMode.Point;
            gifFrame.wrapMode = TextureWrapMode.Clamp;
            gifFrame.SetPixels32(rawTextures[i], 0);
            gifFrame.Apply();
            gifTexList.Add(new GifTexture(gifFrame, decodedFrames[i].FrameDelay));
        }

        if (debugMessages.Count > 0)
        {
            foreach (string debugMessage in debugMessages)
            {
                if (debugMessage.Contains("ERROR"))
                    Debug.LogError(debugMessage);
                else if (debugMessage.Contains("WARNING"))
                    Debug.LogWarning(debugMessage);
                else if (debugMessage.Contains("DEBUG"))
                    Debug.Log(debugMessage);

            }
            debugMessages.Clear();
        }

        DecodingCompleted.Invoke(gifTexList);
    }

    /// <summary>
    /// Set GIF data
    /// </summary>
    /// <param name="gifBytes">GIF byte data</param>
    /// <param name="gifData">ref GIF data</param>
    /// <param name="debugLog">Debug log flag</param>
    /// <returns>Result</returns>
    private static bool SetGifData(byte[] gifBytes, ref GifData gifData, bool debugLog)
    {
        if (debugLog)
            debugMessages.Add($"{DateTime.Now} [DEBUG]: Started setting GifData.");

        if (gifBytes == null || gifBytes.Length <= 0)
        {
            debugMessages.Add($"{DateTime.Now} [ERROR]: Byte data is empty.");
            return false;
        }

        int byteIndex = 0;

        if (SetGifHeader(gifBytes, ref byteIndex, ref gifData) == false)
        {
            debugMessages.Add($"{DateTime.Now} [ERROR]: Problem setting header data.");
            return false;
        }

        if (SetGifBlock(gifBytes, ref byteIndex, ref gifData) == false)
        {
            debugMessages.Add($"{DateTime.Now} [ERROR]: Problem setting gif block data.");
            return false;
        }

        if (debugLog)
        {
            gifData.Dump(debugMessages);
            debugMessages.Add($"{DateTime.Now} [DEBUG]: Completed setting GifData.");
        }
        return true;
    }

    private static bool SetGifHeader(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        // Signature(3 Bytes)
        // 0x47 0x49 0x46 (GIF)
        if (gifBytes[0] != 'G' || gifBytes[1] != 'I' || gifBytes[2] != 'F')
        {
            debugMessages.Add($"{DateTime.Now} [ERROR]: Header data is not valid for a GIF file.");
            return false;
        }
        gifData.m_sig0 = gifBytes[0];
        gifData.m_sig1 = gifBytes[1];
        gifData.m_sig2 = gifBytes[2];

        // Version(3 Bytes)
        // 0x38 0x37 0x61 (87a) or 0x38 0x39 0x61 (89a)
        if ((gifBytes[3] != '8' || gifBytes[4] != '7' || gifBytes[5] != 'a') &&
            (gifBytes[3] != '8' || gifBytes[4] != '9' || gifBytes[5] != 'a'))
        {
            debugMessages.Add($"{DateTime.Now} [ERROR]: GIF version error. Only GIF87a or GIF89a are supported.");
            return false;
        }
        gifData.m_ver0 = gifBytes[3];
        gifData.m_ver1 = gifBytes[4];
        gifData.m_ver2 = gifBytes[5];

        // Logical Screen Width(2 Bytes)
        gifData.m_logicalScreenWidth = BitConverter.ToUInt16(gifBytes, 6);

        // Logical Screen Height(2 Bytes)
        gifData.m_logicalScreenHeight = BitConverter.ToUInt16(gifBytes, 8);

        // 1 Byte
        {
            // Global Color Table Flag(1 Bit)
            gifData.m_globalColorTableFlag = (gifBytes[10] & 128) == 128; // 0b10000000

            // Color Resolution(3 Bits)
            switch (gifBytes[10] & 112)
            {
                case 112: // 0b01110000
                    gifData.m_colorResolution = 8;
                    break;
                case 96: // 0b01100000
                    gifData.m_colorResolution = 7;
                    break;
                case 80: // 0b01010000
                    gifData.m_colorResolution = 6;
                    break;
                case 64: // 0b01000000
                    gifData.m_colorResolution = 5;
                    break;
                case 48: // 0b00110000
                    gifData.m_colorResolution = 4;
                    break;
                case 32: // 0b00100000
                    gifData.m_colorResolution = 3;
                    break;
                case 16: // 0b00010000
                    gifData.m_colorResolution = 2;
                    break;
                default:
                    gifData.m_colorResolution = 1;
                    break;
            }

            // Sort Flag(1 Bit)
            gifData.m_sortFlag = (gifBytes[10] & 8) == 8; // 0b00001000

            // Size of Global Color Table(3 Bits)
            int val = (gifBytes[10] & 7) + 1;
            gifData.m_sizeOfGlobalColorTable = (int)Math.Pow(2, val);
        }

        // Background Color Index(1 Byte)
        gifData.m_bgColorIndex = gifBytes[11];

        // Pixel Aspect Ratio(1 Byte)
        gifData.m_pixelAspectRatio = gifBytes[12];

        byteIndex = 13;
        if (gifData.m_globalColorTableFlag)
        {
            // Global Color Table(0～255×3 Bytes)
            gifData.m_globalColorTable = new List<byte[]>();
            for (int i = byteIndex; i < byteIndex + (gifData.m_sizeOfGlobalColorTable * 3); i += 3)
            {
                gifData.m_globalColorTable.Add(new byte[] { gifBytes[i], gifBytes[i + 1], gifBytes[i + 2] });
            }
            byteIndex = byteIndex + (gifData.m_sizeOfGlobalColorTable * 3);
        }

        return true;
    }

    private static bool SetGifBlock(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        try
        {
            int lastIndex = 0;
            while (true)
            {
                int nowIndex = byteIndex;

                if (gifBytes[nowIndex] == 0x2c)
                {
                    // Image Block(0x2c)
                    SetImageBlock(gifBytes, ref byteIndex, ref gifData);

                }
                else if (gifBytes[nowIndex] == 0x21)
                {
                    // Extension
                    switch (gifBytes[nowIndex + 1])
                    {
                        case 0xf9:
                            // Graphic Control Extension(0x21 0xf9)
                            SetGraphicControlExtension(gifBytes, ref byteIndex, ref gifData);
                            break;
                        case 0xfe:
                            // Comment Extension(0x21 0xfe)
                            SetCommentExtension(gifBytes, ref byteIndex, ref gifData);
                            break;
                        case 0x01:
                            // Plain Text Extension(0x21 0x01)
                            SetPlainTextExtension(gifBytes, ref byteIndex, ref gifData);
                            break;
                        case 0xff:
                            // Application Extension(0x21 0xff)
                            SetApplicationExtension(gifBytes, ref byteIndex, ref gifData);
                            break;
                        default:
                            break;
                    }
                }
                else if (gifBytes[nowIndex] == 0x3b)
                {
                    // Trailer(1 Byte)
                    gifData.m_trailer = gifBytes[byteIndex];
                    byteIndex++;
                    break;
                }

                if (lastIndex == nowIndex)
                {
                    debugMessages.Add($"{DateTime.Now} [ERROR]: Infinite loop detected in GIF block data.");
                    return false;
                }

                lastIndex = nowIndex;
            }
        }
        catch (Exception ex)
        {
            debugMessages.Add($"{DateTime.Now} [ERROR]: {ex.Message}");
            return false;
        }

        return true;
    }

    private static void SetImageBlock(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        ImageBlock ib = new ImageBlock();

        // Image Separator(1 Byte)
        // 0x2c
        ib.m_imageSeparator = gifBytes[byteIndex];
        byteIndex++;

        // Image Left Position(2 Bytes)
        ib.m_imageLeftPosition = BitConverter.ToUInt16(gifBytes, byteIndex);
        byteIndex += 2;

        // Image Top Position(2 Bytes)
        ib.m_imageTopPosition = BitConverter.ToUInt16(gifBytes, byteIndex);
        byteIndex += 2;

        // Image Width(2 Bytes)
        ib.m_imageWidth = BitConverter.ToUInt16(gifBytes, byteIndex);
        byteIndex += 2;

        // Image Height(2 Bytes)
        ib.m_imageHeight = BitConverter.ToUInt16(gifBytes, byteIndex);
        byteIndex += 2;

        // 1 Byte
        {
            // Local Color Table Flag(1 Bit)
            ib.m_localColorTableFlag = (gifBytes[byteIndex] & 128) == 128; // 0b10000000

            // Interlace Flag(1 Bit)
            ib.m_interlaceFlag = (gifBytes[byteIndex] & 64) == 64; // 0b01000000

            // Sort Flag(1 Bit)
            ib.m_sortFlag = (gifBytes[byteIndex] & 32) == 32; // 0b00100000

            // Reserved(2 Bits)
            // Unused

            // Size of Local Color Table(3 Bits)
            int val = (gifBytes[byteIndex] & 7) + 1;
            ib.m_sizeOfLocalColorTable = (int)Math.Pow(2, val);

            byteIndex++;
        }

        if (ib.m_localColorTableFlag)
        {
            // Local Color Table(0～255×3 Bytes)
            ib.m_localColorTable = new List<byte[]>();
            for (int i = byteIndex; i < byteIndex + (ib.m_sizeOfLocalColorTable * 3); i += 3)
            {
                ib.m_localColorTable.Add(new byte[] { gifBytes[i], gifBytes[i + 1], gifBytes[i + 2] });
            }
            byteIndex = byteIndex + (ib.m_sizeOfLocalColorTable * 3);
        }

        // LZW Minimum Code Size(1 Byte)
        ib.m_lzwMinimumCodeSize = gifBytes[byteIndex];
        byteIndex++;

        // Block Size & Image Data List
        while (true)
        {
            // Block Size(1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00)
            {
                // Block Terminator(1 Byte)
                break;
            }

            var imageDataBlock = new ImageBlock.ImageDataBlock();
            imageDataBlock.m_blockSize = blockSize;

            // Image Data(? Bytes)
            imageDataBlock.m_imageData = new byte[imageDataBlock.m_blockSize];
            for (int i = 0; i < imageDataBlock.m_imageData.Length; i++)
            {
                imageDataBlock.m_imageData[i] = gifBytes[byteIndex];
                byteIndex++;
            }

            if (ib.m_imageDataList == null)
            {
                ib.m_imageDataList = new List<ImageBlock.ImageDataBlock>();
            }
            ib.m_imageDataList.Add(imageDataBlock);
        }

        if (gifData.m_imageBlockList == null)
        {
            gifData.m_imageBlockList = new List<ImageBlock>();
        }
        gifData.m_imageBlockList.Add(ib);
    }

    private static void SetGraphicControlExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        GraphicControlExtension gcEx = new GraphicControlExtension();

        // Extension Introducer(1 Byte)
        // 0x21
        gcEx.m_extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Graphic Control Label(1 Byte)
        // 0xf9
        gcEx.m_graphicControlLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size(1 Byte)
        // 0x04
        gcEx.m_blockSize = gifBytes[byteIndex];
        byteIndex++;

        // 1 Byte
        {
            // Reserved(3 Bits)
            // Unused

            // Disposal Mothod(3 Bits)
            // 0 (No disposal specified)
            // 1 (Do not dispose)
            // 2 (Restore to background color)
            // 3 (Restore to previous)
            switch (gifBytes[byteIndex] & 28)
            { // 0b00011100
                case 4:     // 0b00000100
                    gcEx.m_disposalMethod = 1;
                    break;
                case 8:     // 0b00001000
                    gcEx.m_disposalMethod = 2;
                    break;
                case 12:    // 0b00001100
                    gcEx.m_disposalMethod = 3;
                    break;
                default:
                    gcEx.m_disposalMethod = 0;
                    break;
            }

            // User Input Flag(1 Bit)
            // Unknown

            // Transparent Color Flag(1 Bit)
            gcEx.m_transparentColorFlag = (gifBytes[byteIndex] & 1) == 1; // 0b00000001

            byteIndex++;
        }

        // Delay Time(2 Bytes)
        gcEx.m_delayTime = BitConverter.ToUInt16(gifBytes, byteIndex);
        byteIndex += 2;

        // Transparent Color Index(1 Byte)
        gcEx.m_transparentColorIndex = gifBytes[byteIndex];
        byteIndex++;

        // Block Terminator(1 Byte)
        gcEx.m_blockTerminator = gifBytes[byteIndex];
        byteIndex++;

        if (gifData.m_graphicCtrlExList == null)
        {
            gifData.m_graphicCtrlExList = new List<GraphicControlExtension>();
        }
        gifData.m_graphicCtrlExList.Add(gcEx);
    }

    private static void SetCommentExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        CommentExtension commentEx = new CommentExtension();

        // Extension Introducer(1 Byte)
        // 0x21
        commentEx.m_extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Comment Label(1 Byte)
        // 0xfe
        commentEx.m_commentLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size & Comment Data List
        while (true)
        {
            // Block Size(1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00)
            {
                // Block Terminator(1 Byte)
                break;
            }

            var commentDataBlock = new CommentExtension.CommentDataBlock();
            commentDataBlock.m_blockSize = blockSize;

            // Comment Data(n Byte)
            commentDataBlock.m_commentData = new byte[commentDataBlock.m_blockSize];
            for (int i = 0; i < commentDataBlock.m_commentData.Length; i++)
            {
                commentDataBlock.m_commentData[i] = gifBytes[byteIndex];
                byteIndex++;
            }

            if (commentEx.m_commentDataList == null)
            {
                commentEx.m_commentDataList = new List<CommentExtension.CommentDataBlock>();
            }
            commentEx.m_commentDataList.Add(commentDataBlock);
        }

        if (gifData.m_commentExList == null)
        {
            gifData.m_commentExList = new List<CommentExtension>();
        }
        gifData.m_commentExList.Add(commentEx);
    }

    private static void SetPlainTextExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        PlainTextExtension plainTxtEx = new PlainTextExtension();

        // Extension Introducer(1 Byte)
        // 0x21
        plainTxtEx.m_extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Plain Text Label(1 Byte)
        // 0x01
        plainTxtEx.m_plainTextLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size(1 Byte)
        // 0x0c
        plainTxtEx.m_blockSize = gifBytes[byteIndex];
        byteIndex++;

        // Text Grid Left Position(2 Bytes)
        // Not supported
        byteIndex += 2;

        // Text Grid Top Position(2 Bytes)
        // Not supported
        byteIndex += 2;

        // Text Grid Width(2 Bytes)
        // Not supported
        byteIndex += 2;

        // Text Grid Height(2 Bytes)
        // Not supported
        byteIndex += 2;

        // Character Cell Width(1 Bytes)
        // Not supported
        byteIndex++;

        // Character Cell Height(1 Bytes)
        // Not supported
        byteIndex++;

        // Text Foreground Color Index(1 Bytes)
        // Not supported
        byteIndex++;

        // Text Background Color Index(1 Bytes)
        // Not supported
        byteIndex++;

        // Block Size & Plain Text Data List
        while (true)
        {
            // Block Size(1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00)
            {
                // Block Terminator(1 Byte)
                break;
            }

            var plainTextDataBlock = new PlainTextExtension.PlainTextDataBlock();
            plainTextDataBlock.m_blockSize = blockSize;

            // Plain Text Data(n Byte)
            plainTextDataBlock.m_plainTextData = new byte[plainTextDataBlock.m_blockSize];
            for (int i = 0; i < plainTextDataBlock.m_plainTextData.Length; i++)
            {
                plainTextDataBlock.m_plainTextData[i] = gifBytes[byteIndex];
                byteIndex++;
            }

            if (plainTxtEx.m_plainTextDataList == null)
            {
                plainTxtEx.m_plainTextDataList = new List<PlainTextExtension.PlainTextDataBlock>();
            }
            plainTxtEx.m_plainTextDataList.Add(plainTextDataBlock);
        }

        if (gifData.m_plainTextExList == null)
        {
            gifData.m_plainTextExList = new List<PlainTextExtension>();
        }
        gifData.m_plainTextExList.Add(plainTxtEx);
    }

    private static void SetApplicationExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        // Extension Introducer(1 Byte)
        // 0x21
        gifData.m_appEx.m_extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Extension Label(1 Byte)
        // 0xff
        gifData.m_appEx.m_extensionLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size(1 Byte)
        // 0x0b
        gifData.m_appEx.m_blockSize = gifBytes[byteIndex];
        byteIndex++;

        // Application Identifier(8 Bytes)
        gifData.m_appEx.m_appId1 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId2 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId3 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId4 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId5 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId6 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId7 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId8 = gifBytes[byteIndex];
        byteIndex++;

        // Application Authentication Code(3 Bytes)
        gifData.m_appEx.m_appAuthCode1 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appAuthCode2 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appAuthCode3 = gifBytes[byteIndex];
        byteIndex++;

        // Block Size & Application Data List
        while (true)
        {
            // Block Size (1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00)
            {
                // Block Terminator(1 Byte)
                break;
            }

            var appDataBlock = new ApplicationExtension.ApplicationDataBlock();
            appDataBlock.m_blockSize = blockSize;

            // Application Data(n Byte)
            appDataBlock.m_applicationData = new byte[appDataBlock.m_blockSize];
            for (int i = 0; i < appDataBlock.m_applicationData.Length; i++)
            {
                appDataBlock.m_applicationData[i] = gifBytes[byteIndex];
                byteIndex++;
            }

            if (gifData.m_appEx.m_appDataList == null)
            {
                gifData.m_appEx.m_appDataList = new List<ApplicationExtension.ApplicationDataBlock>();
            }
            gifData.m_appEx.m_appDataList.Add(appDataBlock);
        }
    }

    #region Call from DecodeTexture methods

    /// <summary>
    /// Get decoded image data from ImageBlock
    /// </summary>
    private static byte[] GetDecodedData(ImageBlock imgBlock)
    {
        // Combine LZW compressed data
        List<byte> lzwData = new List<byte>();
        for (int i = 0; i < imgBlock.m_imageDataList.Count; i++)
        {
            for (int k = 0; k < imgBlock.m_imageDataList[i].m_imageData.Length; k++)
            {
                lzwData.Add(imgBlock.m_imageDataList[i].m_imageData[k]);
            }
        }

        // LZW decode
        int needDataSize = imgBlock.m_imageHeight * imgBlock.m_imageWidth;
        byte[] decodedData = DecodeGifLZW(lzwData, imgBlock.m_lzwMinimumCodeSize, needDataSize);

        // Sort interlace GIF
        if (imgBlock.m_interlaceFlag)
        {
            decodedData = SortInterlaceGifData(decodedData, imgBlock.m_imageWidth);
        }
        return decodedData;
    }

    /// <summary>
    /// Get color table and set background color (local or global)
    /// </summary>
    private static List<byte[]> GetColorTableAndSetBgColor(GifData gifData, ImageBlock imgBlock, int transparentIndex, out BackgroundColor bgColor)
    {
        List<byte[]> colorTable = imgBlock.m_localColorTableFlag ? imgBlock.m_localColorTable : gifData.m_globalColorTableFlag ? gifData.m_globalColorTable : null;

        if (colorTable != null)
        {
            // Set background color from color table
            byte[] bgRgb = colorTable[gifData.m_bgColorIndex];
            bgColor = new BackgroundColor() { r = bgRgb[0], g = bgRgb[1], b = bgRgb[2], a = (byte)(transparentIndex == gifData.m_bgColorIndex ? 0 : 255) };
        }
        else
        {
            bgColor = new BackgroundColor() { r = 0, g = 0, b = 0, a = 255 };
        }

        return colorTable;
    }

    /// <summary>
    /// Get GraphicControlExtension from GifData
    /// </summary>
    private static GraphicControlExtension? GetGraphicCtrlExt(GifData gifData, int imgBlockIndex)
    {
        if (gifData.m_graphicCtrlExList != null && gifData.m_graphicCtrlExList.Count > imgBlockIndex)
        {
            return gifData.m_graphicCtrlExList[imgBlockIndex];
        }
        return null;
    }

    /// <summary>
    /// Get transparent color index from GraphicControlExtension
    /// </summary>
    private static int GetTransparentIndex(GraphicControlExtension? graphicCtrlEx)
    {
        int transparentIndex = -1;
        if (graphicCtrlEx != null && graphicCtrlEx.Value.m_transparentColorFlag)
        {
            transparentIndex = graphicCtrlEx.Value.m_transparentColorIndex;
        }
        return transparentIndex;
    }

    /// <summary>
    /// Get delay seconds from GraphicControlExtension
    /// </summary>
    private static float GetDelaySec(GraphicControlExtension? graphicCtrlEx)
    {
        // Get delay sec from GraphicControlExtension
        float delaySec = graphicCtrlEx != null ? graphicCtrlEx.Value.m_delayTime / 100f : (1f / 60f);
        if (delaySec <= 0f)
        {
            delaySec = 0.1f;
        }
        return delaySec;
    }

    /// <summary>
    /// Get disposal method from GraphicControlExtension
    /// </summary>
    private static ushort GetDisposalMethod(GraphicControlExtension? graphicCtrlEx)
    {
        return graphicCtrlEx != null ? graphicCtrlEx.Value.m_disposalMethod : (ushort)2;
    }


    /// <summary>
    /// Create Texture2D object and initial settings
    /// </summary>
    private static Texture2D CreateTexture2D(DecodedFrame decodedFrame, int width, int height, int imgIndex, List<GifTexture> gifTexList, List<ushort> disposalMethodList, FilterMode filterMode, TextureWrapMode wrapMode, out bool filledTexture)
    {
        filledTexture = false;

        // Create texture
        Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        tex.filterMode = filterMode;
        tex.wrapMode = wrapMode;

        // Check dispose
        ushort disposalMethod = decodedFrame.DisposalMethod;
        int useBeforeIndex = -1;
        if (disposalMethod == 0)
        {
            // 0 (No disposal specified)
        }
        else if (disposalMethod == 1)
        {
            // 1 (Do not dispose)
            useBeforeIndex = imgIndex - 1;
        }
        else if (disposalMethod == 2)
        {
            // 2 (Restore to background color)
            filledTexture = true;
            Color32[] pix = new Color32[tex.width * tex.height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = new Color32(decodedFrame.BackgroundColor.r, decodedFrame.BackgroundColor.g, decodedFrame.BackgroundColor.b, decodedFrame.BackgroundColor.a);
            }
            tex.SetPixels32(pix);
            tex.Apply();
        }
        else if (disposalMethod == 3)
        {
            // 3 (Restore to previous)
            for (int i = imgIndex - 1; i >= 0; i--)
            {
                if (disposalMethodList[i] == 0 || disposalMethodList[i] == 1)
                {
                    useBeforeIndex = i;
                    break;
                }
            }
        }

        if (useBeforeIndex >= 0)
        {
            filledTexture = true;
            Color32[] pix = gifTexList[useBeforeIndex].m_texture2d.GetPixels32();
            tex.SetPixels32(pix);
            tex.Apply();
        }

        return tex;
    }


    /// <summary>
    /// Set texture pixel row
    /// </summary>
    private static Color32[] GetTextureData(DecodedFrame decodedFrame, ref int dataIndex, bool filledTexture)
    {
        Color32[] pixelData = new Color32[decodedFrame.ImageBlock.m_imageHeight * decodedFrame.ImageBlock.m_imageWidth];

        //y = row, x = column
        //Gif frames start with top left coords, Texture2D with bottom left. Hence the reversal
        for (int y = decodedFrame.ImageBlock.m_imageHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < decodedFrame.ImageBlock.m_imageWidth; x++)
            {
                // Out of image blocks
                if (y < decodedFrame.ImageBlock.m_imageTopPosition ||
                    y >= decodedFrame.ImageBlock.m_imageTopPosition + decodedFrame.ImageBlock.m_imageHeight ||
                    x < decodedFrame.ImageBlock.m_imageLeftPosition ||
                    x >= decodedFrame.ImageBlock.m_imageLeftPosition + decodedFrame.ImageBlock.m_imageWidth)
                {
                    // Get pixel color from bg color
                    if (filledTexture == false)
                    {
                        pixelData[x + y * decodedFrame.ImageBlock.m_imageWidth] = new Color32(
                                decodedFrame.BackgroundColor.r,
                                decodedFrame.BackgroundColor.g,
                                decodedFrame.BackgroundColor.b,
                                decodedFrame.BackgroundColor.a);
                    }
                    continue;
                }

                // Out of decoded data
                if (dataIndex >= decodedFrame.GifData.Length)
                {
                    if (filledTexture == false)
                    {
                        pixelData[x + y * decodedFrame.ImageBlock.m_imageWidth] = new Color32(
                                 decodedFrame.BackgroundColor.r,
                                 decodedFrame.BackgroundColor.g,
                                 decodedFrame.BackgroundColor.b,
                                 decodedFrame.BackgroundColor.a);
                        if (dataIndex == decodedFrame.GifData.Length)
                        {
                            debugMessages.Add($"{DateTime.Now} [ERROR]: dataIndex exceeded the size of decodedData. dataIndex: {dataIndex} decodedData.Length: {decodedFrame.GifData.Length} y: {y} x: {x}");
                        }
                    }
                    dataIndex++;
                    continue;
                }

                // Get pixel color from color table
                {
                    byte colorIndex = decodedFrame.GifData[dataIndex];
                    if (decodedFrame.ColorTable == null || decodedFrame.ColorTable.Count <= colorIndex)
                    {
                        if (filledTexture == false)
                        {
                            pixelData[x + y * decodedFrame.ImageBlock.m_imageWidth] = new Color32(
                                decodedFrame.BackgroundColor.r,
                                decodedFrame.BackgroundColor.g,
                                decodedFrame.BackgroundColor.b,
                                decodedFrame.BackgroundColor.a);
                            if (decodedFrame.ColorTable == null)
                                debugMessages.Add($"{DateTime.Now} [ERROR]: colorIndex exceeded the size of colorTable. colorTable is null. colorIndex: {colorIndex}");
                            else
                                debugMessages.Add($"{DateTime.Now} [ERROR]: colorIndex exceeded the size of colorTable. colorTable.Count: {decodedFrame.ColorTable.Count}. colorIndex: {colorIndex}");
                        }
                        dataIndex++;
                        continue;
                    }
                    byte[] rgb = decodedFrame.ColorTable[colorIndex];

                    // Set alpha
                    byte alpha = decodedFrame.TransparentIndex >= 0 && decodedFrame.TransparentIndex == colorIndex ? (byte)0 : (byte)255;

                    if (filledTexture == false || alpha != 0)
                    {
                        // Set color
                        Color32 col = new Color32(rgb[0], rgb[1], rgb[2], alpha);
                        pixelData[x + y * decodedFrame.ImageBlock.m_imageWidth] = col;
                    }
                }

                dataIndex++;
            }
        }
        return pixelData;
    }


    /// <summary>
    /// Set texture pixel row
    /// </summary>
    private static void SetTexturePixelRow(Texture2D tex, int y, DecodedFrame decodedFrame, ref int dataIndex, bool filledTexture)
    {
        Debug.Log(Time.realtimeSinceStartup);
        // Row no (0~)
        int row = tex.height - 1 - y;

        for (int x = 0; x < tex.width; x++)
        {
            // Line no (0~)
            int line = x;

            // Out of image blocks
            if (row < decodedFrame.ImageBlock.m_imageTopPosition ||
                row >= decodedFrame.ImageBlock.m_imageTopPosition + decodedFrame.ImageBlock.m_imageHeight ||
                line < decodedFrame.ImageBlock.m_imageLeftPosition ||
                line >= decodedFrame.ImageBlock.m_imageLeftPosition + decodedFrame.ImageBlock.m_imageWidth)
            {
                // Get pixel color from bg color
                if (filledTexture == false)
                {
                    tex.SetPixel(x, y, new Color32(
                            decodedFrame.BackgroundColor.r,
                            decodedFrame.BackgroundColor.g,
                            decodedFrame.BackgroundColor.b,
                            decodedFrame.BackgroundColor.a));
                }
                continue;
            }

            // Out of decoded data
            if (dataIndex >= decodedFrame.GifData.Length)
            {
                if (filledTexture == false)
                {
                    tex.SetPixel(x, y, new Color32(
                            decodedFrame.BackgroundColor.r,
                            decodedFrame.BackgroundColor.g,
                            decodedFrame.BackgroundColor.b,
                            decodedFrame.BackgroundColor.a));
                    if (dataIndex == decodedFrame.GifData.Length)
                        debugMessages.Add($"{DateTime.Now} [ERROR]: dataIndex exceeded the size of decodedData. dataIndex: {dataIndex} decodedData.Length: {decodedFrame.GifData.Length} y: {y} x: {x}");
                }
                dataIndex++;
                continue;
            }

            // Get pixel color from color table
            {
                byte colorIndex = decodedFrame.GifData[dataIndex];
                if (decodedFrame.ColorTable == null || decodedFrame.ColorTable.Count <= colorIndex)
                {
                    if (filledTexture == false)
                    {
                        tex.SetPixel(x, y, new Color32(
                            decodedFrame.BackgroundColor.r,
                            decodedFrame.BackgroundColor.g,
                            decodedFrame.BackgroundColor.b,
                            decodedFrame.BackgroundColor.a));
                        if (decodedFrame.ColorTable == null)
                            debugMessages.Add($"{DateTime.Now} [ERROR]: colorIndex exceeded the size of colorTable. colorTable is null. colorIndex: {colorIndex}");
                        else
                            debugMessages.Add($"{DateTime.Now} [ERROR]: colorIndex exceeded the size of colorTable. colorTable.Count: {decodedFrame.ColorTable.Count}. colorIndex: {colorIndex}");
                    }
                    dataIndex++;
                    continue;
                }
                byte[] rgb = decodedFrame.ColorTable[colorIndex];

                // Set alpha
                byte alpha = decodedFrame.TransparentIndex >= 0 && decodedFrame.TransparentIndex == colorIndex ? (byte)0 : (byte)255;

                if (filledTexture == false || alpha != 0)
                {
                    // Set color
                    Color32 col = new Color32(rgb[0], rgb[1], rgb[2], alpha);
                    tex.SetPixel(x, y, col);
                }
            }

            dataIndex++;
        }
    }

    #endregion

    #region Decode LZW & Sort interrace methods

    /// <summary>
    /// GIF LZW decode
    /// </summary>
    /// <param name="compData">LZW compressed data</param>
    /// <param name="lzwMinimumCodeSize">LZW minimum code size</param>
    /// <param name="needDataSize">Need decoded data size</param>
    /// <returns>Decoded data array</returns>
    private static byte[] DecodeGifLZW(List<byte> compData, int lzwMinimumCodeSize, int needDataSize)
    {
        int clearCode = 0;
        int finishCode = 0;

        // Initialize dictionary
        Dictionary<int, string> dic = new Dictionary<int, string>();
        int lzwCodeSize = 0;
        InitDictionary(dic, lzwMinimumCodeSize, out lzwCodeSize, out clearCode, out finishCode);

        // Convert to bit array
        byte[] compDataArr = compData.ToArray();
        var bitData = new BitArray(compDataArr);

        byte[] output = new byte[needDataSize];
        int outputAddIndex = 0;

        string prevEntry = null;

        bool dicInitFlag = false;

        int bitDataIndex = 0;

        // LZW decode loop
        while (bitDataIndex < bitData.Length)
        {
            if (dicInitFlag)
            {
                InitDictionary(dic, lzwMinimumCodeSize, out lzwCodeSize, out clearCode, out finishCode);
                dicInitFlag = false;
            }

            int key = bitData.GetNumeral(bitDataIndex, lzwCodeSize);

            string entry = null;

            if (key == clearCode)
            {
                // Clear (Initialize dictionary)
                dicInitFlag = true;
                bitDataIndex += lzwCodeSize;
                prevEntry = null;
                continue;
            }
            else if (key == finishCode)
            {
                // Exit
                debugMessages.Add($"{DateTime.Now} [WARNING]: LZW Decoding anomaly. Early stop code detected. bitDataIndex: {bitDataIndex} lzwCodeSize: {lzwCodeSize} key: {key} dic.Count: {dic.Count}");
                break;
            }
            else if (dic.ContainsKey(key))
            {
                // Output from dictionary
                entry = dic[key];
            }
            else if (key >= dic.Count)
            {
                if (prevEntry != null)
                {
                    // Output from estimation
                    entry = prevEntry + prevEntry[0];
                }
                else
                {
                    debugMessages.Add($"{DateTime.Now} [WARNING]: LZW Decoding anomaly. Current key did not match previous conditions. bitDataIndex: {bitDataIndex} lzwCodeSize: {lzwCodeSize} key: {key} dic.Count: {dic.Count}");
                    bitDataIndex += lzwCodeSize;
                    continue;
                }
            }
            else
            {
                debugMessages.Add($"{DateTime.Now} [WARNING]: LZW Decoding anomaly. Current key did not match previous conditions. bitDataIndex: {bitDataIndex} lzwCodeSize: {lzwCodeSize} key: {key} dic.Count: {dic.Count}");
                bitDataIndex += lzwCodeSize;
                continue;
            }

            // Output
            // Take out 8 bits from the string.
            byte[] temp = Encoding.Unicode.GetBytes(entry);
            for (int i = 0; i < temp.Length; i++)
            {
                if (i % 2 == 0)
                {
                    output[outputAddIndex] = temp[i];
                    outputAddIndex++;
                }
            }

            if (outputAddIndex >= needDataSize)
            {
                // Exit
                break;
            }

            if (prevEntry != null)
            {
                // Add to dictionary
                dic.Add(dic.Count, prevEntry + entry[0]);
            }

            prevEntry = entry;

            bitDataIndex += lzwCodeSize;

            if (lzwCodeSize == 3 && dic.Count >= 8)
            {
                lzwCodeSize = 4;
            }
            else if (lzwCodeSize == 4 && dic.Count >= 16)
            {
                lzwCodeSize = 5;
            }
            else if (lzwCodeSize == 5 && dic.Count >= 32)
            {
                lzwCodeSize = 6;
            }
            else if (lzwCodeSize == 6 && dic.Count >= 64)
            {
                lzwCodeSize = 7;
            }
            else if (lzwCodeSize == 7 && dic.Count >= 128)
            {
                lzwCodeSize = 8;
            }
            else if (lzwCodeSize == 8 && dic.Count >= 256)
            {
                lzwCodeSize = 9;
            }
            else if (lzwCodeSize == 9 && dic.Count >= 512)
            {
                lzwCodeSize = 10;
            }
            else if (lzwCodeSize == 10 && dic.Count >= 1024)
            {
                lzwCodeSize = 11;
            }
            else if (lzwCodeSize == 11 && dic.Count >= 2048)
            {
                lzwCodeSize = 12;
            }
            else if (lzwCodeSize == 12 && dic.Count >= 4096)
            {
                int nextKey = bitData.GetNumeral(bitDataIndex, lzwCodeSize);
                if (nextKey != clearCode)
                {
                    dicInitFlag = true;
                }
            }
        }

        return output;
    }

    /// <summary>
    /// Initialize dictionary
    /// </summary>
    /// <param name="dic">Dictionary</param>
    /// <param name="lzwMinimumCodeSize">LZW minimum code size</param>
    /// <param name="lzwCodeSize">out LZW code size</param>
    /// <param name="clearCode">out Clear code</param>
    /// <param name="finishCode">out Finish code</param>
    private static void InitDictionary(Dictionary<int, string> dic, int lzwMinimumCodeSize, out int lzwCodeSize, out int clearCode, out int finishCode)
    {
        int dicLength = (int)Math.Pow(2, lzwMinimumCodeSize);

        clearCode = dicLength;
        finishCode = clearCode + 1;

        dic.Clear();

        for (int i = 0; i < dicLength + 2; i++)
        {
            dic.Add(i, ((char)i).ToString());
        }

        lzwCodeSize = lzwMinimumCodeSize + 1;
    }

    /// <summary>
    /// Sort interlace GIF data
    /// </summary>
    /// <param name="decodedData">Decoded GIF data</param>
    /// <param name="xNum">Pixel number of horizontal row</param>
    /// <returns>Sorted data</returns>
    private static byte[] SortInterlaceGifData(byte[] decodedData, int xNum)
    {
        int rowNo = 0;
        int dataIndex = 0;
        var newArr = new byte[decodedData.Length];
        // Every 8th. row, starting with row 0.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 8 == 0)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 8th. row, starting with row 4.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 8 == 4)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 4th. row, starting with row 2.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 4 == 2)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 2nd. row, starting with row 1.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 8 != 0 && rowNo % 8 != 4 && rowNo % 4 != 2)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }

        return newArr;
    }
    #endregion
}