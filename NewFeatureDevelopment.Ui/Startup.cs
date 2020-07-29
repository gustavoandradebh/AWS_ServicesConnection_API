using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewFeatureDevelopment.Application;
using NFD.Application;
using NFD.Domain.Interfaces;
using NFD.Infrastructure;
using NFD.Infrastructure.Interfaces;
using RestEase;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewFeatureDevelopment.Ui
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(opt =>
            {
                opt.AddPolicy("AllowAll", p =>
                {
                    p
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
                });
            })
            .AddHttpClient();

            services.AddDbContext<AmazonRDSContext>(opt =>
                opt.UseSqlServer(
                    Configuration.GetConnectionString("CrossoverDatabase")));

            AWSCredentials cred = new BasicAWSCredentials("AKIAQORDWCRHYI3MI4ER", "yQySAJDfOhDqHKARKmqAeUr3Dy9OJ/Re3BBKJTcU");
            AWSOptions opt = new AWSOptions { Credentials = cred, Region = RegionEndpoint.USEast2 };
            services.AddDefaultAWSOptions(opt);

            services.AddAWSService<IAmazonS3>();
            services.AddSingleton<IAwsS3Service, AwsS3Service>();

            services.AddScoped<IUploadService, UploadAppService>();

            ConfigureElasticsearchAPI(services);

            services.AddScoped<IElasticsearchService, ElasticsearchAppService>();

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.IgnoreNullValues = false;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });
        }

        private void ConfigureElasticsearchAPI(IServiceCollection services)
        {
            var api = RestClient.For<IElasticsearchClient>(Configuration["Elasticsearch:ApiUrlPath"]);
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Configuration["Elasticsearch:Username"]}:{Configuration["Elasticsearch:Password"]}"));
            api.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            services.AddSingleton(x => api);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AmazonRDSContext dbContext)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            dbContext.Database.EnsureCreated();

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseCors("AllowAll");

            app.UseAuthorization();
            app.UseMiddleware(typeof(ExceptionHandlingMiddleware));

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
