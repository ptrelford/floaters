using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Floaters
{
    public class MessageContentProvider : IContentProvider
    {
        public FrameworkElement CreateContent(object state)
        {
            return new TextBlock { Text = state.ToString() };
        }

        public void WriteState(System.Xml.XmlWriter writer, object state)
        {
            writer.WriteElementString("Text", state.ToString());
        }

        public object ReadState(System.Xml.XmlReader reader)
        {
            reader.Read();
            return reader.ReadElementContentAsString();
        }
    }
}
