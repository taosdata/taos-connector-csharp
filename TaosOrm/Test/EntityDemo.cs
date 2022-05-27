using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaosOrm.Test
{
    [Taos("BREAKER_BASIC")]//超级表名
    [Description("时序表")]
    public class EntityDemo
    {
        /// <summary>
        /// 特性含义:别名,类型，类型长度，是否为Tag,是否为表名[表名只能设置一个字段]
        /// </summary>
        [TaosColumn("equipId", TaosDataType.BINARY, "64", true, true)]
        public string EquipId { get; set; }

        [TaosColumn("groupid", TaosDataType.INT, isTag: true)]
        public int GroupId { get; set; }

        [TaosColumn("current", TaosDataType.FLOAT)]
        public double Current { get; set; }

        [TaosColumn("voltage", TaosDataType.INT)]
        public int Voltage { get; set; }

        [TaosColumn("phase", TaosDataType.FLOAT)]
        public double Phase { get; set; }
    }
}
