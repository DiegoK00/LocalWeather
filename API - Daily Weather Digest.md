# API - Daily Weather Digest

Azure Function (isolated worker, .NET 10) che ogni giorno alle **04:00 UTC** genera un
bollettino meteo per una località e lo invia via email come PDF.

## Cosa fa

1. Interroga **Open-Meteo** una volta per ciascun modello configurato (default: ECMWF, GFS, ICON).
2. Calcola la **media ora per ora** di temperatura (°C) e precipitazioni (mm) tra i modelli.
3. Genera un **PDF** con QuestPDF (una sezione per giorno: min/max, totale pioggia, tabella oraria).
4. Invia il PDF via **SMTP** (MailKit) all'indirizzo configurato.

Mediare più modelli Open-Meteo sostituisce lo scraping di siti consumer (iLMeteo/3BMeteo/AccuWeather):
le tabelle orarie di quei siti sono renderizzate via JavaScript e AccuWeather richiede account per le ore estese.

## Struttura

| File | Ruolo |
|------|-------|
| `Functions/DailyWeatherDigestFunction.cs` | Timer trigger `0 0 4 * * *` (UTC), orchestrazione |
| `Services/OpenMeteoWeatherService.cs` | Chiamate Open-Meteo + media multi-modello |
| `Services/QuestPdfWeatherReportBuilder.cs` | Generazione PDF |
| `Services/SmtpEmailSender.cs` | Invio email con allegato |
| `Options/AppOptions.cs` | `WeatherOptions` + `EmailOptions` |
| `Program.cs` | Builder .NET 10 isolated + DI |

## Configurazione

In locale: `local.settings.json` (NON committare).
In Azure: **Configuration → Application settings** della Function App, usando la doppia
underscore per le sezioni (`Weather__Latitude`, `Email__Password`, ...).
I segreti SMTP vanno preferibilmente in **Key Vault** referenziati dalle app settings.

| Setting | Esempio |
|---------|---------|
| `Weather__LocationName` | `Torre Boldone (BG)` |
| `Weather__Latitude` / `Weather__Longitude` | `45.72` / `9.71` |
| `Weather__ForecastDays` | `3` |
| `Weather__TimeZone` | `Europe/Rome` |
| `Weather__Models__0..n` | `ecmwf_ifs025`, `gfs_seamless`, `icon_seamless` |
| `Email__Host` / `Email__Port` | `smtp.example.com` / `587` |
| `Email__User` / `Email__Password` | credenziali SMTP |
| `Email__From` / `Email__To` | mittente / destinatario |

## Esecuzione locale

```bash
dotnet restore
func start          # richiede Azure Functions Core Tools v4 + Azurite per lo storage locale
```

Per testare subito senza aspettare le 4 UTC: impostare temporaneamente `RunOnStartup = true`
sull'attributo `TimerTrigger`, oppure invocare la function via endpoint admin di Functions.

## Deploy

- Piano: **Flex Consumption** (la Linux Consumption non supporta .NET 10; è in dismissione).
- Runtime stack: **.NET 10 isolated**.
- La Function App richiede uno **Storage Account** (usato internamente dal timer per i lock).
- Il timer gira in **UTC** salvo impostare `WEBSITE_TIME_ZONE`. Qui vogliamo 04:00 UTC, quindi default OK.

## Note / TODO

- Email: per ora SMTP. In produzione valutare **Azure Communication Services Email** (nativo Azure).
- Aggiungere retry/resilienza alle chiamate HTTP (es. `Microsoft.Extensions.Http.Resilience`).
- Le versioni dei pacchetti nel `.csproj` sono indicative: verificare le ultime compatibili con .NET 10.
