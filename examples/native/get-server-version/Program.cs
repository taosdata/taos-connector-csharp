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
                           DataBase = "",
                           Username = "root",
                           Password = "taosdata",
                           Port = 6030
            };

            string configDir = "/etc/taos";
            var connection = new TaosConnection(builder.ConnectionString, configDir);
            try
            {
                connection.Open();
                Console.WriteLine("Connected!");
                var cmd = connection.CreateCommand();
                Console.Write("Get server version: ");
                cmd.CommandText = "select server_version()";
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
                Console.WriteLine("Closed!");
            }
        }
    }
}
