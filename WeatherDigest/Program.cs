using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuestPDF.Infrastructure;
using WeatherDigest.Functions;
using WeatherDigest.Options;
using WeatherDigest.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        QuestPDF.Settings.License = LicenseType.Community;

        services.Configure<WeatherOptions>(ctx.Configuration.GetSection(WeatherOptions.SectionName));
        services.Configure<EmailOptions>(ctx.Configuration.GetSection(EmailOptions.SectionName));

        services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>(client =>
        {
            client.BaseAddress = new Uri("https://api.open-meteo.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<IWeatherReportPdfBuilder, QuestPdfWeatherReportBuilder>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddTransient<DailyWeatherDigestFunction>();
    })
    .Build();

var function = host.Services.GetRequiredService<DailyWeatherDigestFunction>();
await function.RunAsync(CancellationToken.None);
