using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Usb.Events;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IUsbEventWatcher _usbEventWatcher;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        _usbEventWatcher = new UsbEventWatcher();
        _usbEventWatcher.UsbDeviceAdded += OnUsbDeviceAdded;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("USB Watcher Service started.");
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("USB Watcher Service stopped.");
        _usbEventWatcher.UsbDeviceAdded -= OnUsbDeviceAdded;
        return Task.CompletedTask;
    }

    private void OnUsbDeviceAdded(object sender, UsbDevice e)
    {
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType == DriveType.Removable && drive.IsReady)
            {
                _logger.LogInformation($"USB Drive detected: {drive.VolumeLabel} ({drive.Name})");
                string targetDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ScrapedSticks", drive.VolumeLabel);
                if (Directory.Exists(targetDirectory))
                {
                    int version = 1;
                    string newTargetDirectory;
                    do
                    {
                        newTargetDirectory = $"{targetDirectory} ({version++})";
                    } while (Directory.Exists(newTargetDirectory));
                    targetDirectory = newTargetDirectory;
                }
                Directory.CreateDirectory(targetDirectory);
                _logger.LogInformation($"Scraping USB Drive to: {targetDirectory}");
                CopyDirectory(drive.RootDirectory.FullName, targetDirectory);
                _logger.LogInformation($"USB Drive scraped to: {targetDirectory}");

            }
        }
    }

    private void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        _logger.LogInformation($"Copying directory: {sourceDir} to {targetDir}");

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string targetFilePath = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, targetFilePath, true);
            _logger.LogInformation($"Copied file: {file} to {targetFilePath}");
        }

        foreach (string directory in Directory.GetDirectories(sourceDir))
        {
            string targetDirectoryPath = Path.Combine(targetDir, Path.GetFileName(directory));
            CopyDirectory(directory, targetDirectoryPath);
        }
    }
}
