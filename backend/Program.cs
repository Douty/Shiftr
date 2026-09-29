using Microsoft.EntityFrameworkCore;
using Shiftr.Controllers;
using Shiftr.Data;
using Shiftr.Interface;
using Shiftr.Repository;
using Shiftr.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddApplicationPart(typeof(EmployeeController).Assembly);
builder.Services.AddDbContext<ShiftrDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();



var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

    app.UseAuthentication();
    app.UseAuthorization();
app.MapControllers();
app.UseHttpsRedirection();

app.Run();


