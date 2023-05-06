using IoTSharp.Data.Taos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Example
{
    public static class issues252
    {
        public static void demo_code(TaosConnectionStringBuilder _builder)
        {
            var builder = new TaosConnectionStringBuilder()
            {
                DataSource = _builder.DataSource,
                DataBase = null,
                Username = _builder.Username,
                Password = _builder.Password,
                Port = 6041,
                Protocol = "RESTful"
            };
            builder.UseRESTful();
            var connection = new TaosConnection(builder.ConnectionString);
            try
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "show databases";
                var res = cmd.ExecuteReader();
                while (res.Read())
                {
                    Console.WriteLine(res.GetString(0));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
