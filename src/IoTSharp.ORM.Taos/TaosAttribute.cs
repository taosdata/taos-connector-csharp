using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTSharp.ORM.Taos
{
    [Serializable]
    public class TaosAttribute : Attribute
    {

        /// <summary>
        /// 超级表
        /// </summary>
        public string SuperTableName { get; }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="superTableName">超级表名</param>     
        public TaosAttribute(string superTableName)
        {
            SuperTableName = superTableName;
        }
    }
    [Serializable]
    public class TaosColumnAttribute : Attribute
    {
        /// <summary>
        /// 列名字
        /// </summary>
        public string ColumnName { get; set; }
        /// <summary>
        /// 列类型
        /// </summary>
        public TaosDataType ColumnType { get; set; }
        public string ColumnLength { get; set; }
        /// <summary>
        /// 是否标签
        /// </summary>
        public bool IsTag { get; set; } = false;
        /// <summary>
        /// 是否为表名
        /// </summary>
        public bool IsTableName { get; set; }
        public TaosColumnAttribute(string columnName, TaosDataType columnType, string columnLength = null, bool isTag = false, bool isTableName = false)
        {
            ColumnName = columnName;
            ColumnType = columnType;
            ColumnLength = columnLength;
            IsTag = isTag;
            IsTableName = isTableName;
        }
    }
}
