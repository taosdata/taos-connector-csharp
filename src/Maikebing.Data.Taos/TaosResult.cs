using System;
using System.Collections.Generic;
using System.Text;

namespace Maikebing.Data.Taos
{
    public class TaosErrorResult
    {
        /// <summary>
        /// 
        /// </summary>
        public string status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string desc { get; set; }
    }
    public class TaosResult
    {
        /// <summary>
        /// 
        /// </summary>
        public string status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> head { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public  object data { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int rows { get; set; }
    }
}
