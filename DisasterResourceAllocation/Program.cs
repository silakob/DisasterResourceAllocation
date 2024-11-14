using DisasterResourceAllocation.Repositories;

string environmentName = Environment.GetEnvironmentVariable("ASPNET_ENVIRONMENT") ??
                         Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                         "";
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile($"appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables().Build();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration["RedisEndPoint"];
    options.InstanceName = "DisasterResourceAllocationRedis";
    options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
    {
        EndPoints = { options.Configuration! },
        AbortOnConnectFail = true
    };
});

builder.Services.AddTransient<IResourceAllocationRepository, ResourceAllocationRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();