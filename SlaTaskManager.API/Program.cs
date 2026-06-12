using Microsoft.EntityFrameworkCore;
using SlaTaskManager.API.Configuration;
using SlaTaskManager.API.Data;
using SlaTaskManager.API.Hubs;
using SlaTaskManager.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SlaTaskManagerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection(RabbitMqSettings.SectionName));
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddSingleton<ITaskEventPublisher, TaskEventPublisher>();
builder.Services.AddSingleton<ISlaStatusCalculator, SlaStatusCalculator>();
builder.Services.AddHostedService<SlaMonitorService>();

builder.Services.AddControllers();
builder.Services.AddSignalR();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
app.MapHub<TaskHub>("/hubs/tasks");

app.Run();
