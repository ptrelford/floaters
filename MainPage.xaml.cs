using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace Floaters
{
    public partial class MainPage : UserControl
    {
        Floaters _floaters;
        int count=1;

        public MainPage()
        {
            InitializeComponent();

            var providers = new Dictionary<string, IContentProvider> { 
                {"Message", new MessageContentProvider() }
            };
            _floaters = new Floaters(this.LayoutRoot, providers );
            TryRestoreWindows();
            _floaters.Updated += (sender, e) => SaveWindows();

        }

        private void NewWindow_Click(object sender, RoutedEventArgs e)
        {
            _floaters.AddFloater("Message", "Title "+count, "Content "+count);
            ++count;
            SaveWindows();
        }

        private void SaveWindows()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists("Windows.xml")) store.DeleteFile("Windows.xml");
                IsolatedStorageFileStream stream = store.CreateFile("Windows.xml");
                var writer = XmlWriter.Create(stream);
                // TODO: save main window top, left, width, height, title
                writer.WriteStartElement("Windows");
                _floaters.Save(writer);
                writer.WriteEndElement();
                writer.Close();
                stream.Close();
            }
        }

        private void TryRestoreWindows()
        {
            try
            {
                RestoreWindows();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void RestoreWindows()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists("Windows.xml"))
                {
                    IsolatedStorageFileStream stream = store.OpenFile("Windows.xml", System.IO.FileMode.Open);
                    var reader = XmlReader.Create(stream);
                    reader.MoveToContent();
                    // TODO: restore main window info
                    _floaters.Restore(reader);
                    stream.Close();
                }
            }
        }

    }
}
