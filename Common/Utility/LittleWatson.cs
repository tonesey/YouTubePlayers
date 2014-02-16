using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using Microsoft.Phone.Tasks;
using Centapp.CartoonCommon.Helpers;


namespace Centapp.CartoonCommon.Utility
{
    public class LittleWatson
    {
        const string _filename = "LittleWatson.txt";

        public static void StoreExceptionDetails(Exception ex, string extra)
        {
            try
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    SafeDeleteFile(store);
                    using (TextWriter output = new StreamWriter(store.CreateFile(_filename)))
                    {
                        output.WriteLine(">>>MESSAGE<<<");
                        output.WriteLine(ex.Message);
                        output.WriteLine(">>>STACKTRACE<<<");
                        output.WriteLine(ex.StackTrace);
                        output.WriteLine(">>>EXTRA_INFO<<<");
                        output.WriteLine(extra);
                    }
                }
            }
            catch
            {
            }
        }

        public static void CheckForPreviousException(string message, string title)
        {
            try
            {
                string contents = null;

                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (store.FileExists(_filename))
                    {
                        using (TextReader reader = new StreamReader(store.OpenFile(_filename, FileMode.Open, FileAccess.Read, FileShare.None)))
                        {
                            contents = reader.ReadToEnd();
                        }
                        SafeDeleteFile(store);
                    }
                }

                if (contents != null)
                {
                    if (MessageBox.Show(message, title, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        EmailComposeTask email = new EmailComposeTask();
                        email.To = "centapp@hotmail.com";
                        email.Subject = string.Format("{0} auto-generated problem report", App.ViewModel.AppName.ToUpper());
                        email.Body = contents;
                        SafeDeleteFile(IsolatedStorageFile.GetUserStoreForApplication());
                        email.Show();
                    }
                }
            }
            catch
            {
            }
            finally
            {
                SafeDeleteFile(IsolatedStorageFile.GetUserStoreForApplication());
            }
        }

        private static void SafeDeleteFile(IsolatedStorageFile store)
        {
            try
            {
                store.DeleteFile(_filename);
            }
            catch
            {
            }
        }
    }
}
