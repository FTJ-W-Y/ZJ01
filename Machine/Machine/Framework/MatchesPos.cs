namespace Machine
{
    #region // 电池状态位

    /// <summary>
    /// 电池状态位：高→低，抓手4→抓手1
    /// </summary>
    public enum BatState
    {
        // 0电芯
        BS_0000,

        // 1电芯
        BS_0001 = 0x01,
        BS_0010 = 0x02,
        BS_0100 = 0x04,
        BS_1000 = 0x08,

        // 2电芯
        BS_0011 = 0x03,
        BS_0101 = 0x05,
        BS_1001 = 0x09,
        BS_1010 = 0x0A,
        BS_0110 = 0x06,
        BS_1100 = 0x0C,

        // 3电芯
        BS_0111 = 0x07,
        BS_1011 = 0x0B,
        BS_1101 = 0x0D,
        BS_1110 = 0x0E,

        // 4电芯
        BS_1111 = 0x0F,
    }
    #endregion

    /// <summary>
    /// 计算匹配位置
    /// </summary>
    public static class MatchesPos
    {
        /// <summary>
        /// 计算匹配位置
        /// </summary>
        /// <param name="pick">暂存true取，false放</param>
        /// <param name="fingBat">抓手电芯</param>
        /// <param name="bufBat">暂存电芯</param>
        /// <param name="bufRow">暂存行位置</param>
        /// <param name="finger">操作的夹爪</param>
        /// <returns>true可以进行操作，false不可以进行操作</returns>
        public static bool CalcPos(bool pick, int fingBat, int bufBat, ref int bufRow, ref int finger)
        {
            #region // 取放原则
            // 1，能一次取够 ModDef.Finger_ALL ，则取
            // 2，移位能一次取完，则取
            // 3，抓手0无电池（包括取后），则放
            // 4，一次不能取够 ModDef.Finger_ALL ，则放
            // 5，抓手0有电池（包括取后）且连续，则可取可放
            // 6，
            #endregion

            int fing0_buf0 = 3;        // 抓手0在暂存0时的行索引

            switch((BatState)fingBat)
            {
                #region // BatState.BS_0000:
                case BatState.BS_0000:
                    if(pick)
                    {
                        switch((BatState)bufBat)
                        {
                            case BatState.BS_0001:
                                if (!pick)
                                {
                                    bufRow = fing0_buf0 + 3;
                                    finger = fingBat;
                                }
                                else
                                {
                                    bufRow = fing0_buf0;
                                    finger = bufBat;
                                }
                                return true;
                            //case BatState.BS_0001:
                            case BatState.BS_0011:
                            case BatState.BS_0111:
                            case BatState.BS_1111:
                                bufRow = fing0_buf0;
                                finger = bufBat;
                                return true;
                            case BatState.BS_0010:
                                if (!pick)
                                {
                                    bufRow = fing0_buf0 + 1;
                                    finger = bufBat >> 1;
                                }
                                else
                                {
                                    bufRow = fing0_buf0 + 1;
                                    finger = bufBat >> 1;
                                }
                                return true;
                            //case BatState.BS_0010: //wjj 220512
                            case BatState.BS_0110:
                            case BatState.BS_1110:
                                bufRow = fing0_buf0 + 1;
                                finger = bufBat >> 1;
                                return true;
                            case BatState.BS_0100:
                            case BatState.BS_1100:
                                bufRow = fing0_buf0 + 2;
                                finger = bufBat >> 2;
                                return true;
                            case BatState.BS_1000:
                                bufRow = fing0_buf0 + 3;
                                finger = bufBat >> 3;
                                return true;
                            case BatState.BS_0101:
                            case BatState.BS_1001:
                            case BatState.BS_1101:
                                bufRow = fing0_buf0;
                                finger = bufBat & (int)BatState.BS_0001;
                                return true;
                            case BatState.BS_1010:
                                bufRow = fing0_buf0 + 1;
                                finger = (bufBat & (int)BatState.BS_0010) >> 1;
                                return true;
                            case BatState.BS_1011:
                                bufRow = fing0_buf0;
                                finger = bufBat & (int)BatState.BS_0011;
                                return true;
                        }
                    }
                    break;
                #endregion

                #region // BatState.BS_0001:
                case BatState.BS_0001:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                            #region //wjj 220506 
                            if (!pick)
                            {
                                bufRow = fing0_buf0 + 3;
                                finger = fingBat;
                                return true;
                            }
                            #endregion //wjj 220506 
                            break;
                        //case BatState.BS_0000:
                        case BatState.BS_0010:
                        case BatState.BS_0110:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0001:
                        case BatState.BS_0101:
                        case BatState.BS_1101:
                        case BatState.BS_1001:
                        case BatState.BS_1100:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0100:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 3;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_1000:
                        case BatState.BS_0011:
                        case BatState.BS_1010:
                        case BatState.BS_1011:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 2;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_1110:
                            bufRow = fing0_buf0;
                            finger = pick ? bufBat : fingBat;
                            return true;
                        case BatState.BS_0111:
                            bufRow = pick ? (fing0_buf0 - 1) : (fing0_buf0 + 3);
                            finger = pick ? (bufBat << 1) : fingBat;
                            return true;
                        case BatState.BS_1111:
                            if(pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = (bufBat & (int)BatState.BS_0111) << 1;
                                return true;
                            }
                            break;
                    }
                    break;
                #endregion

                #region // BatState.BS_0010:
                case BatState.BS_0010:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                        case BatState.BS_0010:
                        case BatState.BS_0110:
                        case BatState.BS_1110:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0001:
                        case BatState.BS_0101:
                        case BatState.BS_1101:
                            bufRow = fing0_buf0;
                            finger = pick ? bufBat : fingBat;
                            return true;
                        case BatState.BS_0100:
                        case BatState.BS_0111:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 2;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_1000:
                        case BatState.BS_0011:
                        case BatState.BS_1010:
                        case BatState.BS_1011:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_1001:
                        case BatState.BS_1100:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat;
                                return true;
                            }
                            break;
                    }
                    break;
                #endregion

                #region // BatState.BS_0100:
                case BatState.BS_0100:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                            if (!pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0010:
                        case BatState.BS_0110:
                        case BatState.BS_1110:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 2;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0001:
                        case BatState.BS_0101:
                        case BatState.BS_1001:
                        case BatState.BS_1100:
                        case BatState.BS_1101:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_1000:
                        case BatState.BS_0011:
                        case BatState.BS_1010:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_1011:
                            bufRow = fing0_buf0;
                            finger = pick ? bufBat : fingBat;
                            return true;
                        case BatState.BS_0100:
                        case BatState.BS_0111:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = fingBat;
                                return true;
                            }
                            break;
                    }
                    break;
                #endregion

                #region // BatState.BS_1000:
                case BatState.BS_1000:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                        case BatState.BS_0100:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0001:
                        case BatState.BS_0101:
                        case BatState.BS_1001:
                        case BatState.BS_1100:
                        case BatState.BS_1101:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 2;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0010:
                        case BatState.BS_0110:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 3;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_1000:
                        case BatState.BS_0011:
                        case BatState.BS_1010:
                        case BatState.BS_1011:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0111:
                            bufRow = fing0_buf0;
                            finger = pick ? bufBat : fingBat;
                            return true;
                        case BatState.BS_1110:
                            bufRow = pick ? (fing0_buf0 + 1) : (fing0_buf0 - 3);
                            finger = pick ? (bufBat >> 1) : fingBat;
                            return true;
                        case BatState.BS_1111:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = (bufBat & 0x0E) >> 1;
                                return true;
                            }
                            break;
                    }
                    break;
                #endregion

                #region // BatState.BS_0011:
                case BatState.BS_0011:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0001:
                        case BatState.BS_1000:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0010:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 2;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0100:
                        case BatState.BS_1100:
                            bufRow = fing0_buf0;
                            finger = pick ? bufBat : fingBat;
                            return true;
                        case BatState.BS_0011:
                            bufRow = pick ? (fing0_buf0 - 2) : (fing0_buf0 + 2);
                            finger = pick ? (bufBat << 2) : fingBat;
                            return true;
                        case BatState.BS_0101:
                        case BatState.BS_1101:
                            if(pick)
                            {
                                bufRow = fing0_buf0 - 2;
                                finger = (bufBat & (int)BatState.BS_0001) << 2;
                                return true;
                            }
                            break;
                        case BatState.BS_1010:
                            if(pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = (bufBat & (int)BatState.BS_0010) << 1;
                                return true;
                            }
                            break;
                        case BatState.BS_0110:
                        case BatState.BS_1110:
                            if(pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = (bufBat & (int)BatState.BS_0110) << 1;
                                return true;
                            }
                            break;
                        case BatState.BS_1001:
                            bufRow = pick ? (fing0_buf0 - 2) : (fing0_buf0 + 1);
                            finger = pick ? ((bufBat & (int)BatState.BS_0001) << 2) : fingBat;
                            return true;
                        case BatState.BS_0111:
                        case BatState.BS_1011:
                        case BatState.BS_1111:
                            if(pick)
                            {
                                bufRow = fing0_buf0 - 2;
                                finger = (bufBat & (int)BatState.BS_0011) << 2;
                                return true;
                            }
                            break;
                    }
                    break;
                #endregion

                #region // BatState.BS_0101:
                case BatState.BS_0101:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat & (int)BatState.BS_0001;
                                return true;
                            }
                            break;
                        case BatState.BS_0001:
                        case BatState.BS_0101:
                            bufRow = pick ? (fing0_buf0 - 1) : (fing0_buf0 + 1);
                            finger = pick ? (bufBat << 1) : fingBat;
                            return true;
                        case BatState.BS_0010:
                        case BatState.BS_1010:
                            bufRow = fing0_buf0;
                            finger = pick ? bufBat : fingBat;
                            return true;
                        case BatState.BS_0100:
                            bufRow = fing0_buf0 + 1;
                            finger = pick ? (bufBat >> 1) : fingBat;
                            return true;
                        case BatState.BS_1000:
                            bufRow = pick ? (fing0_buf0 + 2) : (fing0_buf0);
                            finger = pick ? (bufBat >> 2) : fingBat;
                            return true;
                        case BatState.BS_0011:
                        case BatState.BS_1001:
                        case BatState.BS_1011:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 2;
                                finger = fingBat & (int)BatState.BS_0001;
                                return true;
                            }
                            break;
                        case BatState.BS_0110:
                        case BatState.BS_1110:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 2;
                                finger = fingBat & (int)BatState.BS_0100;
                                return true;
                            }
                            break;
                        case BatState.BS_1100:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = fingBat & (int)BatState.BS_0100;
                                return true;
                            }
                            break;
                        case BatState.BS_0111:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 3;
                                finger = fingBat & (int)BatState.BS_0001;
                                return true;
                            }
                            break;
                        case BatState.BS_1101:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = fingBat & (int)BatState.BS_0100;
                                return true;
                            }
                            break;
                    }
                    break;
                #endregion

                #region // BatState.BS_1001:
                case BatState.BS_1001:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                        case BatState.BS_0010:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat & (int)BatState.BS_0001;
                                return true;
                            }
                            break;
                        case BatState.BS_0001:
                        case BatState.BS_0101:
                        case BatState.BS_1001:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = fingBat & (int)BatState.BS_0001;
                                return true;
                            }
                            break;
                        case BatState.BS_0100:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat & (int)BatState.BS_1000;
                                return true;
                            }
                            break;
                        case BatState.BS_1000:
                        case BatState.BS_0011:
                        case BatState.BS_1010:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = fingBat & (int)BatState.BS_1000;
                                return true;
                            }
                            break;
                        case BatState.BS_0110:
                            bufRow = fing0_buf0;
                            finger = pick ? bufBat : fingBat;
                            return true;
                        case BatState.BS_1100:
                        case BatState.BS_1101:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = (bufBat & (int)BatState.BS_1100) >> 1;
                                return true;
                            }
                            break;
                        case BatState.BS_0111:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 3;
                                finger = fingBat & (int)BatState.BS_0001;
                                return true;
                            }
                            break;
                        case BatState.BS_1011:
                            if(pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = (bufBat & (int)BatState.BS_0011) << 1;
                                return true;
                            }
                            break;
                        case BatState.BS_1110:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 3;
                                finger = fingBat & (int)BatState.BS_1000;
                                return true;
                            }
                            break;
                    }
                    break;
                #endregion

                #region // BatState.BS_1010:
                case BatState.BS_1010:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = fingBat & (int)BatState.BS_0010;
                                return true;
                            }
                            break;
                        case BatState.BS_0001:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat & (int)BatState.BS_0010;
                                return true;
                            }
                            break;
                        case BatState.BS_0010:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0100:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_1000:
                        case BatState.BS_0011:
                        case BatState.BS_1001:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = fingBat & (int)BatState.BS_0010;
                                return true;
                            }
                            break;
                        case BatState.BS_0101:
                            bufRow = fing0_buf0;
                            finger = pick ? bufBat : fingBat;
                            return true;
                        case BatState.BS_1010:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = bufBat >> 1;
                                return true;
                            }
                            break;
                        case BatState.BS_0110:
                            if (!pick)
                            {
                                bufRow = fing0_buf0 + 2;
                                finger = (int)BatState.BS_0010;
                                return true;
                            }
                            break;
                        //case BatState.BS_0110:
                        case BatState.BS_0111:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 2;
                                finger = fingBat & (int)BatState.BS_0001;
                                return true;
                            }
                            break;
                        case BatState.BS_1100:
                        case BatState.BS_1101:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 2;
                                finger = fingBat & (int)BatState.BS_1000;
                                return true;
                            }
                            break;
                        case BatState.BS_1011:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = fingBat & (int)BatState.BS_0001;
                                return true;
                            }
                            break;
                        case BatState.BS_1110:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 3;
                                finger = fingBat & (int)BatState.BS_1000;
                                return true;
                            }
                            break;
                    }
                    break;
                #endregion

                #region // BatState.BS_0110:
                case BatState.BS_0110:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                        case BatState.BS_0100:
                        case BatState.BS_1100:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0001:
                        case BatState.BS_1001:
                            bufRow = fing0_buf0;
                            finger = pick ? bufBat : fingBat;
                            return true;
                        case BatState.BS_0010:
                            bufRow = fing0_buf0 + 1;
                            finger = pick ? (bufBat >> 1) : fingBat;
                            return true;
                        case BatState.BS_1000:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0011:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0101:
                        case BatState.BS_0110:
                        case BatState.BS_0111:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 2;
                                finger = (bufBat & (int)BatState.BS_0100) >> 2;
                                return true;
                            }
                            break;
                        case BatState.BS_1010:
                        case BatState.BS_1110:
                            if(pick)
                            {
                                bufRow = fing0_buf0 - 2;
                                finger = (bufBat & (int)BatState.BS_0010) << 2;
                                return true;
                            }
                            break;
                        case BatState.BS_1011:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 3;
                                finger = (bufBat & (int)BatState.BS_1000) >> 3;
                                return true;
                            }
                            break;
                        case BatState.BS_1101:
                            if(pick)
                            {
                                bufRow = fing0_buf0 - 3;
                                finger = (bufBat & (int)BatState.BS_0001) << 3;
                                return true;
                            }
                            break;
                    }
                    break;
                #endregion

                #region // BatState.BS_1100:
                case BatState.BS_1100:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                        case BatState.BS_0010:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0001:
                        case BatState.BS_1000:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0100:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 2;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0011:
                            bufRow = fing0_buf0;
                            finger = pick ? bufBat : fingBat;
                            return true;
                        case BatState.BS_0101:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = (bufBat & (int)BatState.BS_0100) >> 1;
                                return true;
                            }
                            break;
                        case BatState.BS_1001:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_1010:
                        case BatState.BS_1011:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 2;
                                finger = (bufBat & (int)BatState.BS_1000) >> 2;
                                return true;
                            }
                            break;
                        case BatState.BS_0110:
                        case BatState.BS_0111:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = (bufBat & (int)BatState.BS_0110) >> 1;
                                return true;
                            }
                            break;
                        case BatState.BS_1100:
                        case BatState.BS_1101:
                        case BatState.BS_1110:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 2;
                                finger = (bufBat & (int)BatState.BS_1100) >> 2;
                                return true;
                            }
                            break;
                    }
                    break;
                #endregion

                #region // BatState.BS_0111:
                case BatState.BS_0111:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0001:
                            bufRow = pick ? (fing0_buf0 - 3) : (fing0_buf0 + 1);
                            finger = pick ? ((bufBat & (int)BatState.BS_0001) << 3) : fingBat;
                            return true;
                        case BatState.BS_0010:
                        case BatState.BS_1010:
                        case BatState.BS_0110:
                        case BatState.BS_1110:
                            if(pick)
                            {
                                bufRow = fing0_buf0 - 2;
                                finger = (bufBat & (int)BatState.BS_0010) << 2;
                                return true;
                            }
                            break;
                        case BatState.BS_0100:
                        case BatState.BS_1100:
                            if(pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = (bufBat & (int)BatState.BS_0100) << 1;
                                return true;
                            }
                            break;
                        case BatState.BS_1000:
                            bufRow = fing0_buf0;
                            finger = pick ? bufBat : fingBat;
                            return true;
                        case BatState.BS_0011:
                        case BatState.BS_0101:
                        case BatState.BS_1001:
                        case BatState.BS_0111:
                        case BatState.BS_1011:
                        case BatState.BS_1101:
                            if(pick)
                            {
                                bufRow = fing0_buf0 - 3;
                                finger = (bufBat & (int)BatState.BS_0001) << 3;
                                return true;
                            }
                            break;
                    }
                    break;
                #endregion

                #region // BatState.BS_1011:
                case BatState.BS_1011:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat & (int)BatState.BS_0011;
                                return true;
                            }
                            break;
                        case BatState.BS_0001:
                        case BatState.BS_0101:
                        case BatState.BS_1001:
                        case BatState.BS_1101:
                            if(pick)
                            {
                                bufRow = fing0_buf0 - 2;
                                finger = (bufBat & (int)BatState.BS_0001) << 2;
                                return true;
                            }
                            break;
                        case BatState.BS_0010:
                        case BatState.BS_1010:
                            if(pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = (bufBat & (int)BatState.BS_0010) << 1;
                                return true;
                            }
                            break;
                        case BatState.BS_0100:
                            bufRow = fing0_buf0;
                            finger = pick ? bufBat : fingBat;
                            return true;
                        case BatState.BS_1000:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = (bufBat & (int)BatState.BS_1000) >> 1;
                                return true;
                            }
                            break;
                        case BatState.BS_0011:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 2;
                                finger = fingBat & (int)BatState.BS_0011;
                                return true;
                            }
                            break;
                        case BatState.BS_0110:
                        case BatState.BS_1110:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 3;
                                finger = fingBat & (int)BatState.BS_1000;
                                return true;
                            }
                            break;
                        case BatState.BS_1100:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 2;
                                finger = fingBat & (int)BatState.BS_1000;
                                return true;
                            }
                            break;
                        case BatState.BS_0111:
                        case BatState.BS_1011:
                            // 无法处理
                            break;
                    }
                    break;
                #endregion

                #region // BatState.BS_1101:
                case BatState.BS_1101:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat & (int)BatState.BS_1100;
                                return true;
                            }
                            break;
                        case BatState.BS_0001:
                            if(pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = (bufBat & (int)BatState.BS_0001) << 1;
                                return true;
                            }
                            break;
                        case BatState.BS_0010:
                            bufRow = fing0_buf0;
                            finger = pick ? bufBat : fingBat;
                            return true;
                        case BatState.BS_0100:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = (bufBat & (int)BatState.BS_0100) >> 1;
                                return true;
                            }
                            break;
                        case BatState.BS_1000:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 2;
                                finger = (bufBat & (int)BatState.BS_1000) >> 2;
                                return true;
                            }
                            break;
                        case BatState.BS_0011:
                        case BatState.BS_1010:
                        case BatState.BS_1011:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 2;
                                finger = fingBat & (int)BatState.BS_0001;
                                return true;
                            }
                            break;
                        case BatState.BS_0101:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = fingBat & (int)BatState.BS_0001;
                                return true;
                            }
                            break;
                        case BatState.BS_1001:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 1;
                                finger = fingBat & (int)BatState.BS_1100;
                                return true;
                            }
                            break;
                        case BatState.BS_0110:
                        case BatState.BS_0111:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 + 3;
                                finger = fingBat & (int)BatState.BS_0001;
                                return true;
                            }
                            break;
                        case BatState.BS_1100:
                            if(!pick)
                            {
                                bufRow = fing0_buf0 - 2;
                                finger = fingBat & (int)BatState.BS_1100;
                                return true;
                            }
                            break;
                        case BatState.BS_1101:
                        case BatState.BS_1110:
                            // 无法处理
                            break;
                    }
                    break;
                #endregion

                #region // BatState.BS_1110:
                case BatState.BS_1110:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat;
                                return true;
                            }
                            break;
                        case BatState.BS_0001:
                            bufRow = fing0_buf0;
                            finger = pick ? bufBat : fingBat;
                            return true;
                        case BatState.BS_0010:
                        case BatState.BS_0011:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 1;
                                finger = (bufBat & (int)BatState.BS_0010) >> 1;
                                return true;
                            }
                            break;
                        case BatState.BS_0100:
                        case BatState.BS_0101:
                        case BatState.BS_0110:
                        case BatState.BS_0111:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 2;
                                finger = (bufBat & (int)BatState.BS_0100) >> 2;
                                return true;
                            }
                            break;
                        case BatState.BS_1000:
                            bufRow = pick ? (fing0_buf0 + 3) : (fing0_buf0 - 1);
                            finger = pick ? ((bufBat & (int)BatState.BS_1000) >> 3) : fingBat;
                            return true;
                        case BatState.BS_1001:
                        case BatState.BS_1010:
                        case BatState.BS_1100:
                        case BatState.BS_1011:
                        case BatState.BS_1101:
                        case BatState.BS_1110:
                            if(pick)
                            {
                                bufRow = fing0_buf0 + 3;
                                finger = (bufBat & (int)BatState.BS_1000) >> 3;
                                return true;
                            }
                            break;
                    }
                    break;
                #endregion

                #region // BatState.BS_1111:
                case BatState.BS_1111:
                    switch((BatState)bufBat)
                    {
                        case BatState.BS_0000:
                            if(!pick)
                            {
                                bufRow = fing0_buf0;
                                finger = fingBat;
                                return true;
                            }
                            break;
                    }
                    break;
                    #endregion
            }

            return false;
        }

    }
}
