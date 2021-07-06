using System;
using System.Data;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Database.Auth;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.SkyFireMySqlDatabase.Database;
using WDE.SkyFireMySqlDatabase.Models;

namespace WDE.SkyFireMySqlDatabase
{
    [AutoRegister]
    [SingleInstance]
    public class AuthDatabaseProvider : AuthDatabaseDecorator
    {
        public AuthDatabaseProvider(SkyFireMySqlDatabaseProvider skyfireDatabase,
            NullAuthDatabaseProvider nullAuthDatabaseProvider,
            IAuthDatabaseSettingsProvider settingsProvider,
            IMessageBoxService messageBoxService
        ) : base(nullAuthDatabaseProvider)
        {
            if (settingsProvider.Settings.IsEmpty)
                return;
            
            try
            {
                using var db = new SkyFireAuthDatabase();
                if (db.Connection.State != ConnectionState.Open)
                {
                    db.Connection.Open();
                    db.Connection.Close();   
                }
                impl = skyfireDatabase;
            }
            catch (Exception e)
            {
                impl = nullAuthDatabaseProvider;
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Database error")
                    .SetIcon(MessageBoxIcon.Error)
                    .SetMainInstruction("Couldn't connect to the auth database")
                    .SetContent(e.Message)
                    .WithOkButton(true)
                    .Build());
            }
        }
    }
}