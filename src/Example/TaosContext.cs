using IoTSharp.Data.Taos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaosADODemo
{
    public class  Sensor
    {
        [Key]
        public DateTime ts { get; set; }
        public double degree { get; set; }
        public int pm25 { get; set; }
    }

    public class TaosContext : DbContext
    {
        public TaosContext(DbContextOptions options) : base(options)
        {

        }
        public DbSet<Sensor> Sensor { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            
        }
    }
}
