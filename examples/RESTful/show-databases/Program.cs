using IoTSharp.Data.Taos;

namespace Demo
{
    class Program
    {
        private static void Main()
        {
            var builder = new TaosConnectionStringBuilder()
            {
                DataSource = "127.0.0.1",
                           DataBase = "information_schema",
                           Username = "root",
                           Password = "taosdata",
                           Port = 6041,
                           Protocol = "RESTful"
            };

            string configDir = "/etc/taos";
            var connection = new TaosConnection(builder.ConnectionString, configDir);
            try
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                Console.WriteLine("run SQL command `select name FROM ins_databases` via RESTful connection and outputs: \n");
                cmd.CommandText = "select name from ins_databases";
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
