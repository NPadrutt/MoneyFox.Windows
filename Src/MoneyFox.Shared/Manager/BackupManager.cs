﻿using MoneyFox.Shared.Constants;
using MoneyFox.Shared.Helpers;
using MoneyFox.Shared.Interfaces;
using MvvmCross.Plugins.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OneDrive.Sdk;

namespace MoneyFox.Shared.Manager
{
    /// <summary>
    ///     Manages the backup creation and restore process with different services.
    /// </summary>
    public class BackupManager : IBackupManager
    {
        private readonly IRepositoryManager repositoryManager;
        private readonly IBackupService backupService;
        private readonly IMvxFileStore fileStore;
        private readonly IDatabaseManager databaseManager;

        private bool oldBackupRestored;

        public BackupManager(IRepositoryManager repositoryManager, 
            IBackupService backupService, 
            IMvxFileStore fileStore,
            IDatabaseManager databaseManager)
        {
            this.repositoryManager = repositoryManager;
            this.backupService = backupService;
            this.fileStore = fileStore;
            this.databaseManager = databaseManager;
        }

        /// <summary>
        ///     Gets the backup date from the backup service.
        /// </summary>
        /// <returns>Backupdate.</returns>
        public async Task<DateTime> GetBackupDate()
        {
            if (await CheckIfUserIsLoggedIn())
            {
                return await backupService.GetBackupDate();
            }
            return DateTime.MinValue;
        }
        
        /// <summary>
        ///     Tries to log in the user to the backup service.
        /// </summary>
        ///<exception cref="OneDriveException">Thrown when any error in the OneDrive SDK Occurs</exception>
        public async Task Login()
        {
            await backupService.Login();
        }

        /// <summary>
        ///     Checks if there are files in the backup folder. If yes it assumes that there are backups to restore.
        ///     There is no further check if the files are valid backup files or not.
        /// </summary>
        /// <returns>Backups available or not.</returns>
        public async Task<bool> IsBackupExisting()
        {
            if (await CheckIfUserIsLoggedIn())
            {
                var files = await backupService.GetFileNames();
                return files != null && files.Any();
            }
            return false;
        }

        /// <summary>
        ///     Creates a new backup date.
        /// </summary>
        public async Task CreateNewBackup()
        {
            if (await CheckIfUserIsLoggedIn())
            {
                await backupService.Upload();
            }
        }

        /// <summary>
        ///     Restores an existing backup from the backupservice.
        ///     If it was an old backup, it will delete the existing db an make an migration.
        ///     After the restore it will perform a reload of the data so that the cache works with the new data.
        /// </summary>
        public async Task RestoreBackup()
        {
            if (await CheckIfUserIsLoggedIn())
            {
                var backupNames = GetBackupName(await backupService.GetFileNames());
                await backupService.Restore(backupNames.Item1, backupNames.Item2);

                if (oldBackupRestored && fileStore.Exists(DatabaseConstants.DB_NAME))
                {
                    fileStore.DeleteFile(DatabaseConstants.DB_NAME);
                }

                databaseManager.CreateDatabase();
                databaseManager.MigrateDatabase();

                repositoryManager.ReloadData();
                SettingsHelper.LastDatabaseUpdate = DateTime.Now;
            }
        }

        private Tuple<string, string> GetBackupName(List<string> filenames)
        {
            if (filenames.Contains(DatabaseConstants.BACKUP_NAME))
            {
                return new Tuple<string, string>(DatabaseConstants.BACKUP_NAME, DatabaseConstants.DB_NAME);
            }
            oldBackupRestored = true;
            return new Tuple<string, string>(DatabaseConstants.BACKUP_NAME_OLD, DatabaseConstants.DB_NAME_OLD);
        }

        private async Task<bool> CheckIfUserIsLoggedIn()
        {
            if (backupService.IsLoggedIn)
            {
                return true;
            }

            await backupService.Login();
            return backupService.IsLoggedIn;
        }
    }
}
