using IoTSharp.Data.Taos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IoTSharp.HealthChecks.Taos
{
    public class TaosHealthCheck
         : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _sql;
        public TaosHealthCheck(string sqlserverconnectionstring, string sql)
        {
            _connectionString = sqlserverconnectionstring ?? throw new ArgumentNullException(nameof(sqlserverconnectionstring));
            _sql = sql ?? throw new ArgumentNullException(nameof(sql));
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var connection = new   TaosConnection (_connectionString))
                {
                    await connection.OpenAsync(cancellationToken);

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = _sql;
                      var result=  await command.ExecuteScalarAsync(cancellationToken);
                        if (_sql== TaosHealthCheckBuilderExtensions.HEALTH_QUERY)
                        {
                            var _result = Convert.ToInt32(result);
                            if (_result!=1)
                            {
                                return new HealthCheckResult(context.Registration.FailureStatus,description:$"Server status:{_result}");
                            }
                        }
                    }

                    return HealthCheckResult.Healthy();
                }
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
            }
        }
    }
}
