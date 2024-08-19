using System.Diagnostics;
using System.Globalization;
using BenchmarkDotNet.Attributes;
using Hangfire;
using Hangfire2;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer("Server=VIACHESLAVMW\\SQLEXPRESS;Database=HANGFIRE;Trusted_Connection=True;TrustServerCertificate=True"));

builder.Services.AddHangfire(configuration =>
    configuration.UseSqlServerStorage("Server=VIACHESLAVMW\\SQLEXPRESS;Database=HANGFIRE;Trusted_Connection=True;TrustServerCertificate=True"));


builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseHangfireDashboard("/dashboard");

var test = new Test(app.Services);

// RecurringJob.AddOrUpdate(
//     "move-data-to-dataitems2",
//     () => test.MoveDataToDataItems2(),
//     Cron.Hourly); // Move data every hour
// //
// // BackgroundJob.Enqueue(() => test.MoveDataToDataItems2());

app.MapGet("/", () =>
{
    var jobId = BackgroundJob.Enqueue(() => test.MoveDataToDataItems2(1));
    BackgroundJob.ContinueWith(jobId, () => test.MoveDataToDataItems2(2));
    BackgroundJob.ContinueWith(jobId, () => test.MoveDataToDataItems2(3));
    return Results.Ok();
});

app.MapGet("/ads", () =>
{
    Test s = new(app.Services);
    s.MoveDataToDataItems2(1);
});

app.Run();




public class Test
{
    private readonly IServiceProvider _serviceProvider;

    public Test(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void CreateRecords()
    {
        // using var scope = _serviceProvider.CreateScope();
        // var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        //
        //
        // for (int i = 0; i < 15000; i++)
        // {
        //     dbContext.DataItems.Add(new DataItem
        //     {
        //         SourceColumn = $"SourceValue {i}",
        //         DestinationColumn = string.Empty,
        //         Processed = false
        //     });
        // }
        //
        // dbContext.SaveChanges();
    }
    
    public void MoveDataToDataItems2(int taskNumber)
    {
        var stopwatch = Stopwatch.StartNew();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var itemsToMove = dbContext.DataItems.ToList();

        foreach (var item in itemsToMove)
        {
            dbContext.DataItems2.Add(new DataItem2
            {
                SourceColumn = item.SourceColumn,
                DestinationColumn = item.DestinationColumn,
                Processed = item.Processed
            });

            item.Processed = true; 
        }

        dbContext.SaveChanges();
        
        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        Console.WriteLine($"Task {taskNumber} completed in {elapsedMs} ms");
    }
}

public class DataItem
{
    public int Id { get; set; }
    public string SourceColumn { get; set; }
    public string DestinationColumn { get; set; }
    public bool Processed { get; set; }
}

public class DataItem2
{
    public int Id { get; set; }
    public string SourceColumn { get; set; }
    public string DestinationColumn { get; set; }
    public bool Processed { get; set; }
}

public class MoveDataBenchmark
{
    private Test _test;

    [GlobalSetup]
    public void Setup()
    {
        var serviceProvider = new ServiceCollection()
            .AddDbContext<AppDbContext>(options =>
                options.UseSqlServer("Server=VIACHESLAVMW\\SQLEXPRESS;Database=HANGFIRE;Trusted_Connection=True;TrustServerCertificate=True"))
            .BuildServiceProvider();

        _test = new Test(serviceProvider);
    }

    [Benchmark]
    public void MoveDataToDataItems2Benchmark()
    {
        _test.MoveDataToDataItems2(1);
    }
}
