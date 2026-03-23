var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

// 🔥 REQUIRED PIPELINE ORDER
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/", async context =>
    {
        await context.Response.WriteAsync("API is running 🚀");
    });

    endpoints.MapControllers();
});

app.Run();