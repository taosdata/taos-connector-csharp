using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTSharp.ORM.Taos
{
    /// <summary>
    /// 涛思时序数据库10种数据类型
    /// https://www.bookstack.cn/read/TDengin-2.0-zh/spilt.1.33f53af4c5509954.md
    /// </summary>
    public enum TaosDataType
    {
        TIMESTAMP,
        INT,
        BIGINT,
        FLOAT,
        DOUBLE,
        BINARY,
        SMALLINT,
        TINYINT,
        BOOL,
        NCHAR
    }
}
