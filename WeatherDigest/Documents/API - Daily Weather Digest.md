# API - Daily Weather Digest

Console app .NET 10 che ogni giorno alle **04:00 UTC** genera un bollettino meteo per una
località e lo invia via email come PDF. Schedulata tramite **GitHub Actions** (cron gratuito).

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
| `Functions/DailyWeatherDigestFunction.cs` | Orchestrazione: chiama i servizi e invia il PDF |
| `Services/OpenMeteoWeatherService.cs` | Chiamate Open-Meteo + media multi-modello |
| `Services/QuestPdfWeatherReportBuilder.cs` | Generazione PDF |
| `Services/SmtpEmailSender.cs` | Invio email con allegato |
| `Options/AppOptions.cs` | `WeatherOptions` + `EmailOptions` |
| `Program.cs` | Setup DI + avvio |
| `../.github/workflows/bollettino-meteo.yml` | GitHub Actions: cron `0 4 * * *` (UTC) |

## Configurazione

In locale: `appsettings.local.json` (escluso da git). Sovrascrive `appsettings.json`.
In produzione: **GitHub → Settings → Secrets and variables → Actions** (nome: `WEATHER_*`, `EMAIL_*`).

| Setting / Secret | Esempio |
|------------------|---------|
| `WEATHER_LOCATION_NAME` | `Torre Boldone (BG)` |
| `WEATHER_LATITUDE` / `WEATHER_LONGITUDE` | `45.72` / `9.71` |
| `WEATHER_FORECAST_DAYS` | `3` |
| `WEATHER_TIMEZONE` | `Europe/Rome` |
| `WEATHER_MODEL_0..2` | `ecmwf_ifs025`, `gfs_seamless`, `icon_seamless` |
| `EMAIL_HOST` / `EMAIL_PORT` | `smtp.example.com` / `587` |
| `EMAIL_USER` / `EMAIL_PASSWORD` | credenziali SMTP |
| `EMAIL_FROM` / `EMAIL_TO` | mittente / destinatario |

## Esecuzione locale

```bash
dotnet restore
dotnet run --project WeatherDigest.csproj
```

Per la configurazione locale crea `appsettings.local.json` (non viene committato):

```json
{
  "Email": {
    "Host": "smtp.example.com",
    "User": "...",
    "Password": "...",
    "From": "...",
    "To": "..."
  }
}
```

Per testare subito senza aspettare le 04:00 UTC: da GitHub → Actions → seleziona il workflow
→ **Run workflow** (il trigger `workflow_dispatch` è abilitato).

## Deploy

1. Crea un repo GitHub e fai il push della cartella `Meteo/` (o la root del progetto).
2. Vai su **Settings → Secrets and variables → Actions** e aggiungi tutti i secret della tabella sopra.
3. Il workflow parte automaticamente alle 04:00 UTC ogni giorno.

Non serve nessun server, nessuna carta di credito. GitHub Actions è gratuito fino a 2.000 minuti/mese
(ogni run dura ~30 secondi, quindi ci vorrebbe anni per esaurirli).

## Note / TODO

- Aggiungere retry/resilienza alle chiamate HTTP (es. `Microsoft.Extensions.Http.Resilience`).
- Email: valutare servizi SMTP gratuiti come **Brevo** (ex Sendinblue) o **Resend** se non si ha già un server SMTP.
