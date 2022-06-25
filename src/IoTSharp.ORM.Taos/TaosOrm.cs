using IoTSharp.Data.Taos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IoTSharp.ORM.Taos
{
    /// <summary>
    /// 时序数据库Orm 查询未实现
    /// </summary>
    public class TaosOrm
    {
        private readonly TaosConnection _taos;
        private static readonly ConcurrentDictionary<string, TaosStructure> _tableStructure =    new ConcurrentDictionary<string, TaosStructure> ();// 表名缓存
  
        public TaosOrm(string connectionString)
        {
            _taos = new TaosConnection(connectionString);
            if (_taos.State != System.Data.ConnectionState.Open) _taos.Open();
        }
        public TaosOrm(TaosConnection _taos)
        {
            if (_taos.State != System.Data.ConnectionState.Open) _taos.Open();
        }
        /// <summary>
        /// 数据新增
        /// </summary>
        /// <typeparam name="TEntity">实体对象</typeparam>
        /// <param name="entity"></param>
        /// <param name="checkDB">为True 检查超级表结构是否存在不存在就创建</param>
        /// <returns></returns>
        public async Task<int> TaosAddAsync<TEntity>(TEntity entity, bool checkDB = false) where TEntity : class
        {
            //获取超级表结构
            var st = GetSuperTaosStructure(entity);
            //获取值列表
            StringBuilder tagValue = new StringBuilder();
            StringBuilder columnValue = new StringBuilder();
            PropertyInfo[] infos = entity.GetType().GetProperties();
            if (checkDB) CheckDataBase(st, infos);//监测表结构是否存在
            TaosColumnAttribute dfAttr = null;
            object[] dfAttrs;
            int tags = 0;
            int columns = 0;
            foreach (PropertyInfo info in infos)
            {
                dfAttrs = info.GetCustomAttributes(typeof(TaosColumnAttribute), false);
                if (dfAttrs.Length > 0)
                {
                    dfAttr = dfAttrs[0] as TaosColumnAttribute;
                    if (dfAttr is TaosColumnAttribute)
                    {
                        if (dfAttr.IsTag)
                        {
                            if (dfAttr.IsTableName)//是不是为表名的字段
                            {
                                st.TableName = info.GetValue(entity, null).ToString();
                            }
                            //tagName.Append(tags > 0 ? "," + dfAttr.ColumnName : dfAttr.ColumnName);
                            if (dfAttr.ColumnType == TaosDataType.BINARY || dfAttr.ColumnType == TaosDataType.NCHAR)//字符串就得加''
                            {
                                tagValue.Append(tags > 0 ? "," + $"'{info.GetValue(entity, null)}'" : $"'{info.GetValue(entity, null)}'");
                            }
                            else
                            {
                                tagValue.Append(tags > 0 ? "," + info.GetValue(entity, null) : info.GetValue(entity, null));
                            }
                            tags++;
                        }
                        else
                        {
                            //columnName.Append(columns > 0 ? "," + dfAttr.ColumnName : dfAttr.ColumnName);
                            if (dfAttr.ColumnType == TaosDataType.BINARY || dfAttr.ColumnType == TaosDataType.NCHAR)//字符串就得加''
                            {
                                columnValue.Append(columns > 0 ? "," + $"'{info.GetValue(entity, null)}'" : $"'{info.GetValue(entity, null)}'");
                            }
                            else
                            {
                                columnValue.Append(columns > 0 ? "," + info.GetValue(entity, null) : info.GetValue(entity, null));
                            }
                            columns++;
                        }
                    }
                }
            }
            if (st.TableName == null) throw new Exception("未设备表名特性");
            string strInsertSQL = $@"INSERT INTO D_{st.TableName} USING  
           {st.SuperTableName}
            ({string.Join(",", st.TagNames)}) TAGS({tagValue})
            (ts,{string.Join(",", st.ColumnNames)}) VALUES(now,{columnValue})";
#if DEBUG
            Console.WriteLine($"TaosSql:{strInsertSQL}");
#endif
            return await _taos.CreateCommand(strInsertSQL).ExecuteNonQueryAsync();
        }
        /// <summary>
        /// 缓存超级表结构 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static TaosStructure GetSuperTaosStructure<TEntity>(TEntity entity) where TEntity : class
        {
            Type entityType = entity.GetType();
            PropertyInfo[] infos = entityType.GetProperties();
            TaosStructure dt = _tableStructure.ContainsKey(entityType.FullName) ? _tableStructure[entityType.FullName] : null;
            if (dt == null)
            {
                TaosStructure taos = new TaosStructure();
                TaosColumnAttribute dfAttr = null;
                object[] dfAttrs;
                if (entityType.GetCustomAttributes(typeof(TaosAttribute), false)[0] is TaosAttribute dtAttr)
                {
                    taos.SuperTableName = dtAttr.SuperTableName;
                }
                else
                {
                    throw new Exception(entityType.ToString() + "未设置DataTable特性。");
                }
                foreach (PropertyInfo info in infos)
                {
                    dfAttrs = info.GetCustomAttributes(typeof(TaosColumnAttribute), false);
                    if (dfAttrs.Length > 0)
                    {
                        dfAttr = dfAttrs[0] as TaosColumnAttribute;
                        if (dfAttr is TaosColumnAttribute)
                        {
                            if (dfAttr.IsTag)
                            {
                                taos.TagNames.Add(dfAttr.ColumnName);
                            }
                            else
                            {
                                taos.ColumnNames.Add(dfAttr.ColumnName);
                            }
                        }
                    }
                }
                _tableStructure[entityType.FullName] = taos;
                return taos;
            }
            return dt;
        }
        /// <summary>
        /// 创建DB与超级表
        /// </summary>
        /// <param name="st"></param>
        /// <param name="infos"></param>
        private void CheckDataBase(TaosStructure st, PropertyInfo[] infos)
        {
            //backlog:切换DB不能实现 有BUG
            _taos.CreateCommand($"CREATE DATABASE IF NOT EXISTS {_taos.Database} KEEP 365 DAYS 10 BLOCKS 4;").ExecuteNonQuery();
            _taos.ChangeDatabase(_taos.Database);//创建后切换DB
            StringBuilder columnTag = new StringBuilder();
            StringBuilder column = new StringBuilder();
            TaosColumnAttribute dfAttr = null;
            object[] dfAttrs;
            int tags = 0;
            int columns = 0;
            foreach (PropertyInfo info in infos)
            {
                dfAttrs = info.GetCustomAttributes(typeof(TaosColumnAttribute), false);
                if (dfAttrs.Length > 0)
                {
                    dfAttr = dfAttrs[0] as TaosColumnAttribute;
                    if (dfAttr is TaosColumnAttribute)
                    {
                        string length = "";
                        if (dfAttr.ColumnLength != null)
                        {
                            length = $"({dfAttr.ColumnLength})";//判断数据类型是否有长度有些没有长度
                        }
                        if (dfAttr.IsTag)//是标签
                        {
                            string tag = $"{dfAttr.ColumnName} {dfAttr.ColumnType}{length}";
                            columnTag.Append(tags > 0 ? "," + tag : tag);
                            tags++;
                        }
                        else
                        {
                            string tag = $"{dfAttr.ColumnName} {dfAttr.ColumnType}{length}";
                            column.Append(columns > 0 ? "," + tag : tag);
                            columns++;
                        }
                    }
                }
            }
            //创建超级表
            _taos.CreateCommand($@"CREATE TABLE IF NOT EXISTS {_taos.Database}.{st.SuperTableName}
            (`ts` TIMESTAMP,
            {column}) TAGS ({columnTag});")
                         .ExecuteNonQuery();
        }


    }
    public class TaosStructure
    {
        public string TableName { get; set; }//表名是动态值 (为设备ID)
        public string SuperTableName { get; set; }
        public List<string> TagNames { get; set; } = new List<string>();
        public List<string> ColumnNames { get; set; } = new List<string>();
    }
}
