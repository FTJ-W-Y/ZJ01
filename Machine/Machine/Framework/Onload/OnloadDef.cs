namespace Machine
{
    /// <summary>
    /// 夹具在位状态
    /// </summary>
    enum HaveFake
    {
        Invalid = 0,                // 未知
        Have,                       // 有
        Not,                        // 无

    }

    /// <summary>
    /// 允许取料
    /// </summary>
    enum PickState
    {
        Invalid = 0,                // 未知
        Have,                       // 允许
        Not,                        // 不允许

    }
    /// <summary>
    /// 允许放料
    /// </summary>
    enum PlaceState
    {
        Invalid = 0,                // 未知
        Have,                    // 允许
        Not,                       // 不允许

    }
    /// <summary>
    /// 上料机器人安全位
    /// </summary>
    enum AvoidState
    {
        Invalid = 0,                // 未知
        Have,                  // 安全
        Not,                     // 非安全
    }
    /// <summary>
    /// 清尾料
    /// </summary>
    enum ClearFlag
    {
        Invalid = 0,                // 未知
        Have,                   // 有清尾料
        Not,                      // 无清尾料
    }
    /// <summary>
    /// 夹具信息传输完成标志
    /// </summary>
    enum EndFlag
    {
        Invalid = 0,                // 未知
        Have,                   // 完成
        Not,                      // 非完成
    }
    /// <summary>
    /// 夹具信息传输完成标志
    /// </summary>
    enum RunStateEnum
    {
        Stop = 0,                // 停止
        Run,                   // 运行
        Alarm,                      // 报警
    }
    /// <summary>
    /// 夹具状态
    /// </summary>
    enum PalletStates
    {
        Invalid = 0,                // 未知
        OK,              // 有效OK状态
        NG,              // 有效NG状态
        Detect,          // 待检测状态
        WaitResult,      // 等待结果（已取走假电池）
        WaitOffload,     // 等待下料
        ReputFake,       // 假电池回炉（水含量超标，待放回假电池）
        Rebaking,        // 等待二次干燥（水含量超标，已放回假电池）
    }
    /// <summary>
    /// 电池状态
    /// </summary>
    enum BatteryState
    {
        Invalid = 0,                // 未知
        Not,                        // 无夹具
        WaitWork,                   // 待组盘
        Working,                    // 组盘中
        WorkEnd,                    // 组盘完成
        WaitTurn,                   // NG待转盘
        Turning,                      // NG转盘中
        TurnEnd,                        //NG转盘完成
        WaitFake,                        //待上假电池
        Fakeing,                    //上假电池中
        FakeEnd,                    //上假电池完成
    }
    /// <summary>
    /// 上料命令索引
    /// </summary>
    public enum LoadingCmd
    {
        RunState = 0,            // 运行状态与IO信号（读）

        PalletUpCodeOne,                 // 夹具1电池条码前50个（读）
        PalletMidCodeOne,                  // 夹具1电池条码中50个（读）
        PalletDownCodeOne,                    // 夹具1电池条码后50个（读）
        PalletOne,                   // 夹具1电池信息（读）

        PalletUpCodeTwo,                 // 夹具2电池条码前50个（读）
        PalletMidCodeTwo,               // 夹具2电池条码中50个（读）
        PalletDownCodeTwo,              // 夹具2电池条码后50个（读）
        PalletTwo,                            // 夹具2电池信息（读）

        PalletUpCodeThree,                   // 夹具3电池条码前50个（读）
        PalletMidCodeThree,                 // 夹具3电池条码中50个（读）
        PalletDownCodeThree,                // 夹具3电池条码后50个（读）
        PalletThree,                         // 夹具3电池信息（读）

        ReadBarcode,                 // 电池扫码（读）

        ReadOnload,                 // 上料允许取放夹具（读）

        ReadPickPalletCode,         //读取上料来料料框数据

        ReadMoudle,                 // 模组IO信号（读）

        ReadOrWriteHeartBeat,                 // 心跳（读/写）

        ReadState,                 // 机台状态（读）

        ReadScanAlram,                // 扫码枪报警（读）

        ReadRobotStopAlram,           //上料机器人防撞报警（读）

        WriteRoleID,                 // 权限下发（写）
        WriteOPName,                 // 操作员人名（写）

        WriteTrans,                 // 调度请求取放夹具（写）
        WriteTransAvoid,                 // 调度请求取放夹具（写）
        WriteBarcodeFlag,                 // 电池扫码结束复位（写）
        WritePalletFlag,               //来料料框扫码结束复位（写）
        WriteBarcode,                 // 电池扫码（写）
        WritePalletCodeUp,                 // 夹具信息写入前50个（写）
        WritePalletCodeMid,                 // 夹具信息写入中50个（写）
        WritePalletCodeDown,                 // 夹具信息写入后50个（写）
        WritePallet,                 // 电池状态（写）
        WriteReBattery,              // 复投电池（写)

        WriteTranSaft,                 // 调度机器人安全位信号（写）

        WriteInfoEnd,                   //信息传输完成


        End,
    }

    /// <summary>
    /// 命令结构
    /// </summary>
    public struct OnloadCmdAddr
    {
        public ZoneCode zone;         // 区域
        public short wordAddr;        // 字起地址
        public short wordInterval;    // 字地址间隔
        public short bitAddr;         // 位起地址
        public short bitInterval;     // 位地址间隔
        public short count;           // 总数量

        public OnloadCmdAddr(ZoneCode zoneCode, short wordStartAddr, short wordAddrInterval, short bitStartAddr, short bitAddrInterval, short addrCount)
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
