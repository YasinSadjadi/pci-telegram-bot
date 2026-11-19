using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VocabularyBot.Application.Workflows;
using VocabularyBot.Infrastructure;

public sealed class WeeklyBackupWorker(
    IServiceScopeFactory scopes,
    TelegramClient telegram,
    ILogger<WeeklyBackupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                var tehran = TehranTime.Local(DateTime.UtcNow);
                var daysSinceSaturday = ((int)tehran.DayOfWeek + 1) % 7;
                var scheduled = tehran.Date.AddDays(-daysSinceSaturday).AddHours(9);
                if (scheduled > tehran)
                    scheduled = scheduled.AddDays(-7);

                await SendPendingBackups(DateOnly.FromDateTime(scheduled), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Weekly database backup failed. It will be retried.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SendPendingBackups(DateOnly week, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDb>();
        var source = (SqliteConnection)db.Database.GetDbConnection();
        var directory = Path.GetDirectoryName(Path.GetFullPath(source.DataSource))!;
        var receiptPath = Path.Combine(directory, "weekly-backup-deliveries.json");
        var receipt = File.Exists(receiptPath)
            ? JsonSerializer.Deserialize<DeliveryReceipt>(await File.ReadAllTextAsync(receiptPath, ct))
            : null;
        if (receipt?.Week != week)
            receipt = new DeliveryReceipt(week, []);

        var admins = await db.Users.Where(user => user.IsAdmin && user.TelegramId > 0)
            .Select(user => user.TelegramId).Distinct().ToListAsync(ct);
        admins.RemoveAll(receipt.AdminIds.Contains);
        if (admins.Count == 0)
            return;

        var workingDirectory = Directory.CreateTempSubdirectory("vocabulary-backup-");
        try
        {
            var snapshotPath = Path.Combine(workingDirectory.FullName, "vocabulary.db");
            var zipPath = Path.Combine(workingDirectory.FullName, "backup.zip");
            await db.Database.OpenConnectionAsync(ct);
            try
            {
                await using var snapshot = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = snapshotPath,
                    Pooling = false
                }.ToString());
                await snapshot.OpenAsync(ct);
                source.BackupDatabase(snapshot);
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }

            await using (var zip = await ZipFile.OpenAsync(zipPath, ZipArchiveMode.Create, ct))
            {
                await zip.CreateEntryFromFileAsync(snapshotPath, "vocabulary.db", CompressionLevel.Optimal, ct);
            }

            var fileName = $"vocabulary-backup-{week.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.zip";
            foreach (var id in admins)
            {
                try
                {
                    await telegram.SendDocument(id, zipPath, fileName, ct);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    logger.LogWarning("Weekly backup delivery to admin {AdminId} failed ({Failure}).",
                        id, ex is HttpRequestException { StatusCode: { } status } ? status.ToString() : ex.GetType().Name);
                    continue;
                }

                receipt.AdminIds.Add(id);
                await File.WriteAllTextAsync(receiptPath + ".tmp", JsonSerializer.Serialize(receipt), CancellationToken.None);
                File.Move(receiptPath + ".tmp", receiptPath, overwrite: true);
            }
        }
        finally
        {
            workingDirectory.Delete(recursive: true);
        }
    }

    private sealed record DeliveryReceipt(DateOnly Week, HashSet<long> AdminIds);
}
