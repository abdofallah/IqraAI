using ProjectIqraBackend.App;

namespace ProjectIqraBackend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            /** Services START **/

            IqraApp iqraApp = new IqraApp(builder);
            await iqraApp.Initialize();
            iqraApp.AddServicesToSingleton();

            /** Services END **/

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapControllers();
            app.Run();
        }
    }
}
