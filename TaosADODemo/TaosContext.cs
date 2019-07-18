using Maikebing.Data.Taos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaosADODemo
{
    public class  sensor
    {
        [Key]
        public DateTime ts { get; set; }
        public double? degree { get; set; }
        public int? pm25 { get; set; }
    }

    public class TaosContext : DbContext
    {
        public DbSet<sensor> sensor { get; set; }
 

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string database = "db";
            var builder = new TaosConnectionStringBuilder()
            {
                DataSource = "http://td.gitclub.cn/rest/sql",
                DataBase = database,
                 Username="root",
                  Password= "taosdata"
            };

            optionsBuilder
                .UseTaos(builder.ConnectionString,opt=>
                {
                  
                });
        }
    }
}
