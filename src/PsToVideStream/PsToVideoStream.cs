namespace PsToVideStream;
public class PsToVideoStream
{
    private List<byte> psBuffer;

    private List<byte>? videoStreams;

    private int psDataLength;
    private int offset = 0;
    private NextParseState parseState = NextParseState.PackHeader;
    /// <summary>
    /// 找寻并去除包头
    /// </summary>
    /// <returns>True解析继续 False解析停止</returns>
    private bool RemovePackHeader()
    {
        if (psBuffer[offset++] == 0x00 && psBuffer[offset++] == 0x00
               && psBuffer[offset++] == 0x01 && psBuffer[offset++] == 0xBA)
        {
            //包头最少有14个字节，第14个字节的最后3bit说明了包头14字节后填充数据的长度
            offset += 10;
            var pack_stuffing_length = psBuffer[offset - 1] & 0x07;
            //判断包头长度是否为完整包头
            if (psDataLength >= offset + pack_stuffing_length)
            {
                //移除包头并继续下一步
                offset = offset + pack_stuffing_length;
                psBuffer.RemoveRange(0, offset);
                psDataLength -= offset;
                parseState = NextParseState.SystemHeader;
                offset = 0;
                return true;
            }
        }
        parseState = NextParseState.Default;
        offset = 0;
        return false;
    }
    /// <summary>
    /// 找寻并移除系统头
    /// </summary>
    /// <returns>True解析继续 False解析停止</returns>
    private bool RemoveSystemHeader()
    {
        //判断是否包含系统头
        if (psBuffer[offset++] == 0x00 && psBuffer[offset++] == 0x00
             && psBuffer[offset++] == 0x01 && psBuffer[offset++] == 0xBB)
        {
            var pack_stuffing_length = psBuffer[offset++] * 256 + psBuffer[offset++];
            offset += pack_stuffing_length;
            //包含系统头，判断系统头是否完整
            if (psDataLength > offset)
            {
                //系统头完整，去除系统头，解析继续
                psBuffer.RemoveRange(0, offset);
                psDataLength -= offset;
                parseState = NextParseState.ProgramStreamMap;
                offset = 0;
                return true;
            }
            //系统头不完整，解析停止
            this.parseState = NextParseState.SystemHeader;
            offset = 0;
            return false;
        }
        //不包含系统头，解析继续
        parseState = NextParseState.ProgramStreamMap;
        offset = 0;
        return true;

    }

    /// <summary>
    /// 找寻并移除Program Stream Map
    /// </summary>
    /// <returns>True解析继续 False解析停止</returns>
    private bool RemoveProgramStreamMap()
    {
        //判断是否包含Program Stream Map
        if (psBuffer[offset++] == 0x00 && psBuffer[offset++] == 0x00
            && psBuffer[offset++] == 0x01 && psBuffer[offset++] == 0xBC)
        {
            var strmapLength = psBuffer[offset++] * 256 + psBuffer[offset++];
            offset += strmapLength;
            //包含Program Stream Map，判断Program Stream Map是否完整
            if (psDataLength > offset)
            {
                //Program Stream Map完整，去除Program Stream Map，解析继续
                psBuffer.RemoveRange(0, offset);
                psDataLength -= offset;
                parseState = NextParseState.Pes;
                offset = 0;
                return true;
            }

            //Program Stream Map不完整，解析停止
            this.parseState = NextParseState.ProgramStreamMap;
            offset = 0;
            return false;
        }
        // 不包含Program Stream Map，解析继续
        this.parseState = NextParseState.Pes;
        offset = 0;
        return true;
    }
    /// <summary>
    /// 00 00 01 E0 开始
    /// 00 1A 8C 80 0A 
    /// 共5字节，2字节PES包长度是00 1A，表示此PES数据包的长度是0x001a 即26字节；2字节标准位信息是8C 80
    /// 5字节中的最后一字节表示附加数据长度是0A，跟在附加数据长度后的就是视频数据负载了。
    /// 21 1C C9 AE 0D FF FF FF FF FC(附加数据)
    /// </summary>
    /// <returns></returns>
    private bool ParsingPakcet()
    {
        try
        {
            //判断包头
            if (psBuffer[offset++] == 0x00 && psBuffer[offset++] == 0x00
                    && psBuffer[offset++] == 0x01)
            {
                if (psBuffer[offset] == 0xE0)
                {
                    offset++;
                    ///2字节表示长度
                    var pesPackLength = psBuffer[offset++] * 256 + psBuffer[offset++];
                    byte[] tempBuffer = new byte[pesPackLength];
                    if (psDataLength < offset + pesPackLength)
                    {
                        //丢掉并且结束解码
                        parseState = NextParseState.Pes;
                        psBuffer.RemoveRange(0, psDataLength);
                        psDataLength = 0;
                        offset = 0;
                        return true;
                    }
                    Buffer.BlockCopy(psBuffer.ToArray(), offset, tempBuffer, 0, pesPackLength);
                    psBuffer.RemoveRange(0, offset + pesPackLength);
                    psDataLength = psDataLength - offset - tempBuffer.Length;
                    offset = 0;
                    #region 8C=140=(10 00 1 1 00): 首先是固定值10
                    ///8C=140=(10 00 1 1 00): 首先是固定值10
                    ///接下来的两位为(PES加扰控制字段)PES_scrambling_control,这里是00，表示没有加扰(加密)。剩下的01,10,11由用户自定义。
                    ///接下来第4位为PES优先级字段(PES_priority),当为1时为高优先级，0为低优先级。这里为1。
                    ///接下来第3位为(数据对齐指示符字段)PESdata_alignment_indicator,
                    ///接下来第2位为版权位，
                    ///接下来第1位为版权位，
                    #endregion
                    var pes = tempBuffer[offset++];
                    #region 80=128=(10 000000)
                    //80=128=(10 000000):
                    //首先是PTS,DTS标志字段,这里是10，表示有PTS,没有DTS。
                    //接下来第6位是ESCR标志字段，这里为0，表示没有该段
                    //接下来第5位是ES速率标志字段，，这里为0，表示没有该段
                    //接下来第4位是DSM特技方式标志字段，，这里为0，表示没有该段
                    //接下来第3位是附加版权信息标志字段，，这里为0，表示没有该段
                    //接下来第2位是PES CRC标志字段，，这里为0，表示没有该段
                    //接下来第1位是PES扩展标志字段，，这里为0，表示没有该段
                    #endregion
                    var ptsdts = tempBuffer[offset++];
                    //附加数据长度
                    var appendDataLength = tempBuffer[offset++];
                    offset += appendDataLength;
                    var h264Data = new byte[tempBuffer.Length - offset];
                    Buffer.BlockCopy(tempBuffer, offset, h264Data, 0, h264Data.Length);

                    parseState = NextParseState.Pes;
                    videoStreams?.AddRange(h264Data);
                    h264Data = null;
                    offset = 0;
                    return true;
                }
                else if (psBuffer[offset] == 0xC0 || psBuffer[offset] == 0xBD)//0xC0音频;0xBD海康私有协议
                {
                    offset++;
                    var pesPackLength = psBuffer[offset++] * 256 + psBuffer[offset++];
                    psBuffer.RemoveRange(0, offset + pesPackLength);
                    psDataLength = psDataLength - offset - pesPackLength;
                    parseState = NextParseState.Pes;
                    offset = 0;
                    return true;
                }
            }
            //数据包错误，寻找包头,解析继续
            offset = 0;
            parseState = NextParseState.PackHeader;
            return true;
        }
        catch (Exception ex)
        {
            parseState = NextParseState.PackHeader;
            return false;
        }



    }
    public List<byte> GeVideoStreamFromPs(List<byte> psData)
    {
        this.psBuffer = psData;
        videoStreams = new List<byte>();
        psDataLength = this.psBuffer.Count;
        //循环解析
        while (psDataLength >= 4)
        {
            switch (this.parseState)
            {
                case NextParseState.PackHeader:
                    RemovePackHeader();
                    break;
                case NextParseState.SystemHeader:
                    RemoveSystemHeader();
                    break;
                case NextParseState.ProgramStreamMap:
                    RemoveProgramStreamMap();
                    break;
                case NextParseState.Pes:
                    ParsingPakcet();
                    break;
                default:
                    this.parseState = NextParseState.Default;
                    break;
            }
        }
        psBuffer.Clear();
        return videoStreams;
    }
}
