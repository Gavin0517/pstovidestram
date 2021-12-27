#region << 版 本 注 释 >>
/*
 * ----------------------------------------------------------------
 * Copyright @南京新广云信息科技有限公司 2021. All rights reserved.
 
 * 作    者 ：Gavin 
 
 * 创建时间 ：2021/12/27 16:30:33
 
 * CLR 版本 ：4.0.30319.42000
 
 * 命名空间 ：PsToVideStream
 
 * 类 名 称 ：Class1

 * 类 描 述 ：
 
 * ------------------------------------------------------
 * 历史更新记录
 
 * 版本 ：  V1.0.0.0        修改时间：2021/12/27 16:30:33         修改人：Gavin 
 
 * 修改内容：
 * 
 */
#endregion


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PsToVideStream
{
    internal enum NextParseState
    {
        /// <summary>
        /// 包头
        /// </summary>
        PackHeader = 0,
        /// <summary>
        /// 系统头
        /// </summary>
        SystemHeader = 1,
        /// <summary>
        /// 流程
        /// </summary>
        ProgramStreamMap = 2,
        /// <summary>
        /// pes 数据
        /// </summary>
        Pes = 3,
        /// <summary>
        /// 其他
        /// </summary>
        Default = 255,
    }
}
