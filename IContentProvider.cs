using System.Windows;
using System.Xml;

namespace Floaters
{
    public interface IContentProvider
    {
        FrameworkElement CreateContent(object state);
        void WriteState(XmlWriter writer, object state);
        object ReadState(XmlReader reader);
    }
}
