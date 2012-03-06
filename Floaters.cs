using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace Floaters
{
    public class Floaters
    {
        private readonly Canvas _parent;
        private readonly IDictionary<string, IContentProvider> _providers;
        private readonly
            Dictionary<FloatingControl, string> _floaters =
            new Dictionary<FloatingControl, string>();

        public event EventHandler Updated;

        public Floaters(Canvas parent, IDictionary<string, IContentProvider> providers)
        {
            _parent = parent;
            _providers = providers;
        }

        private FrameworkElement CreateContent(string type, object state)
        {
            var provider = _providers[type];
            var content = provider.CreateContent(state);
            return content;
        }

        public FloatingControl AddFloater(string type, string title, object state,
            int top = 100, int left = 200, int width = 200, int height = 100, bool isExternal = false)
        {
            var floater = new FloatingControl(_parent);
            floater.Width = width;
            floater.Height = height;
            floater.Title = title;
            floater.Data = state;
            floater.DataContext = floater;
            var content = _providers[type].CreateContent(state);
            floater.Body.Content = content;
            floater.Moved += new EventHandler(floater_Moved);
            floater.Closed += new EventHandler(win_Closed);
            _floaters.Add(floater, type);
            if (isExternal && Application.Current.IsRunningOutOfBrowser)
            {
                var window = new Window()
                {
                    WindowStyle = WindowStyle.None,
                    Title = title,
                    Width = width,
                    Height = height,
                    Top = top,
                    Left = left,
                    Content = floater
                };
                window.Show();
                floater.Closed += new EventHandler(floater_Closed);
            }
            else
            {
                Canvas.SetTop(floater, top);
                Canvas.SetLeft(floater, left);
                _parent.Children.Add(floater);
            }
            return floater;
        }

        void floater_Closed(object sender, EventArgs e)
        {
            var window = Window.GetWindow(sender as DependencyObject);
            if (window != null)
            {
                window.Close();
            }
        }

        void floater_Moved(object sender, EventArgs e)
        {
            RaiseUpdate();
        }

        private void RaiseUpdate()
        {
            var updated = Updated;
            if (updated != null) updated(this, new EventArgs());
        }

        void win_Closed(object sender, EventArgs e)
        {
            var floater = sender as FloatingControl;
            floater.Moved -= new EventHandler(floater_Moved);
            floater.Closed -= new EventHandler(win_Closed);
            // Close window
            RemoveFloater(floater);
            RaiseUpdate();
        }

        public void RemoveFloater(FloatingControl floater)
        {
            _parent.Children.Remove(floater);
            _floaters.Remove(floater);
        }

        public void Save(XmlWriter writer)
        {
            foreach(var item in _floaters)
            {
                var type = item.Value;
                var floater = item.Key;
                SaveWindow(writer, type, floater);
            }
        }

        private void SaveWindow(XmlWriter writer, string type, FloatingControl floater)
        {
            writer.WriteStartElement("Window");
            writer.WriteAttributeString("Type", type);
            writer.WriteAttributeString("Title", floater.Title);
            writer.WriteAttributeString("Top", ((int)floater.Top).ToString());
            writer.WriteAttributeString("Left", ((int)floater.Left).ToString());
            writer.WriteAttributeString("Width", ((int)floater.Width).ToString());
            writer.WriteAttributeString("Height", ((int)floater.Height).ToString());
            writer.WriteAttributeString("IsExternal", floater.IsExternal.ToString());
            var provider = _providers[type];
            provider.WriteState(writer, floater.Data);
            writer.WriteEndElement();
        }

        public void Restore(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element &&
                    reader.Name == "Window")
                {
                    RestoreWindow(reader);
                }
            }
        }

        private void RestoreWindow(XmlReader reader)
        {
            string type = reader.GetAttribute("Type");
            string title = reader.GetAttribute("Title");
            int top = int.Parse(reader.GetAttribute("Top"));
            int left = int.Parse(reader.GetAttribute("Left"));
            int width = int.Parse(reader.GetAttribute("Width"));
            int height = int.Parse(reader.GetAttribute("Height"));
            bool isExternal = bool.Parse(reader.GetAttribute("IsExternal"));
            reader.Read();
            var state = _providers[type].ReadState(reader.ReadSubtree());
            AddFloater(type, title, state, top, left, width, height, isExternal);
        }
    }
}
