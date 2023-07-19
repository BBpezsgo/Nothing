using System.Xml.Linq;

namespace AssetManager
{
    public class XmlLoader
    {
        internal static XElement Load(string path) => XElement.Load(path);
    }
}
