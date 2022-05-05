using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTSharp.Data.Taos.Driver
{
    public class DatabaseSchema
    {
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string created_time { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int ntables { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int vgroups { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int replica { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int quorum { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int days { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string keep { get; set; }
     
        [Column("cache(MB)")]
        public int cache  { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int blocks { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int minrows { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int maxrows { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int wallevel { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int fsync { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int comp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int cachelast { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string precision { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int update { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string status { get; set; }
    }

}
