namespace WeatherDigest.Options;

/// <summary>Parametri della località e dei modelli previsionali da mediare.</summary>
public sealed class WeatherOptions
{
    public const string SectionName = "Weather";

    public string LocationName { get; set; } = "Torre Boldone (BG)";
    public double Latitude { get; set; } = 45.72;
    public double Longitude { get; set; } = 9.71;

    /// <summary>Numero di giorni di previsione (oggi incluso). 3 = oggi, domani, dopodomani.</summary>
    public int ForecastDays { get; set; } = 3;

    /// <summary>Fuso orario IANA per allineare le ore restituite da Open-Meteo.</summary>
    public string TimeZone { get; set; } = "Europe/Rome";

    /// <summary>Modelli previsionali Open-Meteo di cui calcolare la media (le "fonti").</summary>
    public string[] Models { get; set; } =
    [
        "ecmwf_ifs025",
        "gfs_seamless",
        "icon_seamless"
    ];
}

/// <summary>Parametri SMTP per l'invio del bollettino via email.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}
