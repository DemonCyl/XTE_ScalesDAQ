using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTE_ScalesDAQ.Entity
{
    public class ConfigData
    {
        #region 数据库配置
        public string DataIpAddress { get; set; }

        public string DataBaseName { get; set; }

        public string Uid { get; set; }

        public string Pwd { get; set; }
        #endregion

        /// <summary>
        /// 磅秤编号
        /// </summary>
        public int FWNo { get; set; }

        /// <summary>
        /// 串口号
        /// </summary>
        public string PortName { get; set; }
        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get; set; }
    }
}
