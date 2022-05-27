using System;
using TaosOrm.Test;

namespace TaosOrm
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TaosSugar taosSugar = new TaosSugar();
            _ = taosSugar.TaosAddAsync(
                new EntityDemo()
                {
                    Current = 2,
                    Phase = 30,
                    Voltage = 4,
                    EquipId = "3afbe36dd5b740a6b7c1634b70687d51",
                    GroupId = 1
                }, true);
            Console.WriteLine("Hello World!");
        }
    }
}
