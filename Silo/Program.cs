using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.Configuration;
using System.Net;

namespace Silo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseOrleans((hostBuilderContext, siloBuilder) =>
                {
                    var configuration = hostBuilderContext.Configuration;

                    siloBuilder.UseLocalhostClustering()
                        .Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromMinutes(1))
                        .Configure<ClusterOptions>(configuration.GetSection("Orleans"))
                        //.Configure<SiloOptions>(options => options.SiloName = "Silo1")
                        .Configure<EndpointOptions>(opts =>
                        {
                            opts.AdvertisedIPAddress = IPAddress.Loopback;
                            opts.SiloPort = configuration.GetValue<int>("Orleans:SiloPort");
                            opts.GatewayPort = configuration.GetValue<int>("Orleans:GatewayPort");
                        })
                        // ʵ��grain�־û�
                        .AddAdoNetGrainStorage("demo", options =>
                        {
                            options.ConnectionString = configuration.GetConnectionString("MySQL");
                            options.Invariant = "MySql.Data.MySqlClient";
                            options.UseJsonFormat = true;
                        })
                        // ͨ��Consulʵ�ּ�Ⱥ����
                        //.UseConsulClustering(options =>
                        //{
                        //    options.Address = new Uri(configuration.GetValue<string>("Consul:Address"));
                        //});
                        // ͨ�����ݿ�ʵ�ּ�Ⱥ����
                        .UseAdoNetClustering(options =>
                        {
                            options.ConnectionString = configuration.GetConnectionString("MySQL");
                            options.Invariant = "MySql.Data.MySqlClient";
                        });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
