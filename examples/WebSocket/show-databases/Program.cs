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
                           DataBase = null, 
                           Username = "root",
                           Password = "taosdata",
                           Port = 6041,
                           Protocol = "WebSocket"
            };

            string configDir = "/etc/taos";
            var connection = new TaosConnection(builder.ConnectionString, configDir);
            try
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                Console.WriteLine("run SQL command `show databases` via WebSocket connection and outputs: \n");
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
