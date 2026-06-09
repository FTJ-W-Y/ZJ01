namespace Machine
{
    /// <summary>
    /// 下料命令索引
    /// </summary>
    public enum OffloadCmd
    {
        RunState=0,            // 运行状态与IO信号（读）
        PalletUpCodeOne,                 // 夹具1电池条码前50个（读）
        PalletMidCodeOne,                  // 夹具1电池条码中50个（读）
        PalletDownCodeOne,                    // 夹具1电池条码后50个（读）

        ReadPalletOne,                 // 夹具1信息（读）

        PalletUpCodeTwo,                 // 夹具2电池条码前50个（读）
        PalletMidCodeTwo,               // 夹具2电池条码中50个（读）
        PalletDownCodeTwo,              // 夹具2电池条码后50个（读）
        ReadPalletTwo,                 // 夹具2信息（读）

        ReadOffload,                  // 下料允许取放信号（读）

        ReadOrWriteHeartBeat,                 // 心跳（读/写）

        ReadState,                 // 机台状态（读）

        ReadFingerAlram,                //下料机械手报警（读）
        ReadPalletAlram,                //下料夹具报警（读）

        WriteRoleID,                 // 权限下发（写）
        WriteOPName,                 // 操作员人名（写）

        WriteTrans,                  // 调度避让（写）

        WritePalletCodeUp,                 // 夹具信息写入前50个（写）
        WritePalletCodeMid,                 // 夹具信息写入中50个（写）
        WritePalletCodeDown,                 // 夹具信息写入后50个（写）
        WritePallet,                 // 电池扫码（写）

        WriteTranSaft,                 // 调度机器人安全位信号（写）

        WriteInfoEnd,                   //信息传输完成
        End,
    }

    /// <summary>
    /// 命令结构
    /// </summary>
    public struct OffloadCmdAddr
    {
        public ZoneCode zone;         // 区域
        public short wordAddr;        // 字起地址
        public short wordInterval;    // 字地址间隔
        public short bitAddr;         // 位起地址
        public short bitInterval;     // 位地址间隔
        public short count;           // 总数量

        public OffloadCmdAddr(ZoneCode zoneCode, short wordStartAddr, short wordAddrInterval, short bitStartAddr, short bitAddrInterval, short addrCount)
        {
            this.zone = zoneCode;
            this.wordAddr = wordStartAddr;
            this.wordInterval = wordAddrInterval;
            this.bitAddr = bitStartAddr;
            this.bitInterval = bitAddrInterval;
            this.count = addrCount;
        }
    };

}
