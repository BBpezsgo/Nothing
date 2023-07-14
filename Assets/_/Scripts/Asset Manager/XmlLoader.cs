using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using UnityEngine.UIElements;

public class XmlLoader
{
    internal static XElement Load(string path) => XElement.Load(path);
}
