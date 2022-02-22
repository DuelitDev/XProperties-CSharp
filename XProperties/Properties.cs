using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

namespace XProperties;

/// <summary>
/// Parse .properties file.
/// </summary>
public class Properties : IEnumerable<string>
{
    private readonly Dictionary<string, string> _properties;

    public Properties()
    {
        _properties = new Dictionary<string, string>();
    }
        
    /// <summary>
    /// Set/Getting the value of a property.
    /// </summary>
    /// <param name="key">Property name.</param>
    /// <exception cref="KeyNotFoundException">
    /// When property does not exist.
    /// </exception>
    /// <returns>Property value.</returns>
    public string this[string key]
    {
        get => GetProperty(key);
        set => SetProperty(key, value);
    }

    /// <summary>
    /// Get an iterator object.
    /// </summary>
    /// <returns>Iterator object.</returns>
    public IEnumerator<string> GetEnumerator()
    {
        return _properties.Keys.GetEnumerator();
    }
        
    /// <summary>
    /// Get an iterator object.
    /// </summary>
    /// <returns>Iterator object.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
        
    /// <summary>
    /// Get the number of properties.
    /// </summary>
    /// <returns>The number of properties.</returns>
    public int Count => _properties.Count;
        
    /// <summary>
    /// Convert the Properties object to string.
    /// </summary>
    /// <param name="prop">Properties object.</param>
    /// <returns>Converted string.</returns>
    public static implicit operator string(Properties prop)
    {
        return prop.ToString();
    }
        
    /// <summary>
    /// Convert the Properties object to dictionary.
    /// </summary>
    /// <param name="prop">Properties object.</param>
    /// <returns>Converted dictionary.</returns>
    public static implicit operator
        Dictionary<string, string>(Properties prop)
    {
        return prop._properties;
    }

    /// <summary>
    /// Loads a property list(key and element pairs)
    /// from the .properties file.
    /// The file is assumed to use the
    /// ISO-8859-1(Latin1) character encoding.
    /// </summary>
    /// <param name="fileName">.properties file name.</param>
    /// <exception cref="IOException">
    /// When an error occurs while reading the file.
    /// </exception>
    /// <exception cref="FormatException">
    /// When the file contains a malformed Unicode escape sequence.
    /// </exception>
    public void Load(string fileName)
    {
        using var fs = new FileStream(fileName, FileMode.Open);
        using var reader = new StreamReader(fs, Encoding.Latin1);
        var temp = reader.ReadToEnd();
        temp = Regex.Replace(temp, "^[#!].*[\r\n\f]+", "");
        temp = Regex.Replace(
            temp, "(?<!\\\\)\\\\[ \t]*[\r\n\f]+[ \t]*", "");
        var rawData = Regex.Split(temp, "[\r\n\f]+");
        foreach (var i in rawData)
        {
            var regex = new Regex("(?<!\\\\)[ \t]*(?<!\\\\)[=:][ \t]*");
            var pair = regex.Split(i, 2);
            if (pair[0].Trim() == string.Empty) continue;
            var key = LoadConvert(pair[0], true);
            if (pair.Length == 2)
            {
                var value = LoadConvert(pair[1]);
                _properties.Add(key, value);
            }
            else
            {
                _properties.Add(key, "");
            }
        }
    }
        
    /// <summary>
    /// Saves a property list(key and element pairs)
    /// to the .properties file.
    /// The file will be written in
    /// ISO-8859-1(Latin1) character encoding.
    /// </summary>
    /// <param name="fileName">.properties file name.</param>
    /// <exception cref="IOException">
    /// When an error occurs while writing data.
    /// </exception>
    public void Save(string fileName)
    {
        using var fs = new FileStream(fileName, FileMode.Create);
        using var writer = new StreamWriter(fs, Encoding.Latin1);
        foreach (var (k, v) in _properties)
        {
            var key = SaveConvert(k, true);
            var value = SaveConvert(v);
            var pair = key + "=" + value;
            writer.WriteLine(pair);
        }
    }
        
    /// <summary>
    /// Loads a property list(key and element pairs) from the xml file.
    ///
    /// The XML document must have the following DOCTYPE declaration:
    /// &lt;!DOCTYPE properties SYSTEM
    /// "http://java.sun.com/dtd/properties.dtd"&gt;
    /// </summary>
    /// <param name="fileName">.xml file name.</param>
    /// <exception cref="IOException">
    /// When an error occurs while reading the file.
    /// </exception>
    /// <exception cref="FormatException">
    /// When the XML file is malformed.
    /// </exception>
    public void LoadFromXml(string fileName)
    {
        var doc = new XmlDocument();
        try
        {
            doc.Load(fileName);
        }
        catch (XmlException)
        {
            throw new FormatException("Malformed XML format.");
        }
        var pairs = doc.GetElementsByTagName("entry");
        foreach (XmlNode pair in pairs)
        {
            if (pair.Attributes?["key"] != null)
            {
                var key = pair.Attributes["key"]!.Value;
                var value = pair.InnerText;
                _properties[key] = value;
            }
            else
            {
                throw new FormatException("Malformed XML format.");
            }
        }
    }
        
    /// <summary>
    /// Saves a property list(key and element pairs) to the .xml file.
    /// 
    /// The XML document must have the following DOCTYPE declaration:
    /// &lt;!DOCTYPE properties SYSTEM
    /// "http://java.sun.com/dtd/properties.dtd"&gt;
    /// </summary>
    /// <param name="fileName">.xml file name.</param>
    /// <exception cref="IOException">
    /// When an error occurs while writing data.
    /// </exception>
    public void SaveToXml(string fileName)
    {
        var doc = new XmlDocument();
        var doctype = doc.CreateDocumentType(
            "properties", 
            null, 
            "http://java.sun.com/dtd/properties.dtd", 
            null
        );
        doc.AppendChild(doctype);
        var root = doc.CreateElement("properties");
        foreach (var (k, v) in _properties)
        {
            var element = doc.CreateElement("entry");
            var attribute = doc.CreateAttribute("key");
            attribute.Value = k;
            element.Attributes.Append(attribute);
            element.InnerText = v;
            root.AppendChild(element);
        }
        doc.AppendChild(root);
        using var fs = new FileStream(fileName, FileMode.Create);
        using var writer = new StreamWriter(fs, Encoding.UTF8);
        doc.Save(writer);
    }
        
    /// <summary>
    /// Converts escape sequences to chars.
    /// </summary>
    /// <param name="value">String to convert.</param>
    /// <param name="isConvertKey">Whether to convert the key.</param>
    /// <exception cref="FormatException">
    /// When the value contains a malformed Unicode escape sequence.
    /// </exception>
    /// <returns>Converted string.</returns>
    private static string LoadConvert(
        string value, bool isConvertKey = false)
    {
        if (isConvertKey)
        {
            value = value.Replace("\\ ", " ");
            value = value.Replace("\\=", "=");
            value = value.Replace("\\:", ":");
        }
        value = value.Replace("\\\\", "\\");
        value = value.Replace("\\t", "\t");
        value = value.Replace("\\r", "\r");
        value = value.Replace("\\n", "\n");
        value = value.Replace("\\f", "\f");
        var escapes = Regex.Matches(value, "\\\\u[A-F0-9]{4}");
        foreach (Match escape in escapes)
        {
            var temp = 0;
            foreach (var i in escape.Value[2..])
            {
                if ("1234567890".Contains(i))
                    temp = (temp << 4) + i - '0';
                else if ("abcdef".Contains(i))
                    temp = (temp << 4) + 10 + i - 'a';
                else if ("ABCDEF".Contains(i))
                    temp = (temp << 4) + 10 + i - 'A';
                else
                    throw new FormatException(
                        "Malformed \\uxxxx encoding.");
            }
            value = value.Replace(escape.Value, ((char)temp).ToString());
        }

        return value;
    }
        
    /// <summary>
    /// Converts chars to escape sequences.
    /// </summary>
    /// <param name="value">String to convert.</param>
    /// <param name="isConvertKey">Whether to convert the key.</param>
    /// <returns>Converted string.</returns>
    private static string SaveConvert(
        string value, bool isConvertKey = false)
    {
        var buffer = new List<string>();
        if (isConvertKey)
        {
            value = value.Replace(" ", "\\ ");
            value = value.Replace("=", "\\=");
            value = value.Replace(":", "\\:");
        }
        value = value.Replace("\\", "\\\\");
        value = value.Replace("\t", "\\t");
        value = value.Replace("\r", "\\r");
        value = value.Replace("\n", "\\n");
        value = value.Replace("\f", "\\f");
        foreach (var chr in value)
        {
            if (chr < 0x20 || chr > 0x7e)
            {
                var newChr = "\\u" + Convert.ToByte(chr).ToString("X4");
                buffer.Add(newChr);
                continue;
            }
            buffer.Add(chr.ToString());
        }
        return string.Join("", buffer);
    }

    /// <summary>
    /// Setting the value of a property.
    /// </summary>
    /// <param name="key">Property name.</param>
    /// <param name="value">Value to set for the property.</param>
    public void SetProperty(string key, string value)
    {
        _properties[key] = value;
    }
        
    /// <summary>
    /// Getting the value of a property.
    /// </summary>
    /// <param name="key">Property name.</param>
    /// <param name="default">
    /// Default value if property does not exist.
    /// </param>
    /// <exception cref="KeyNotFoundException">
    /// When property does not exist.
    /// </exception>
    /// <returns>Property Value.</returns>
    public string GetProperty(string key, string @default = "")
    {
        if (!_properties.ContainsKey(key) && @default != string.Empty)
            return @default;
        return _properties[key];
    }
        
    /// <summary>
    /// Deleting the value of a property.
    /// </summary>
    /// <param name="key">Property name.</param>
    /// <exception cref="KeyNotFoundException">
    /// When property does not exist.
    /// </exception>
    public void DeleteProperty(string key)
    {
        _properties.Remove(key);
    }

    /// <summary>
    /// Remove all properties.
    /// </summary>
    public void Clear()
    {
        _properties.Clear();
    }
        
    /// <summary>
    /// Getting the list of properties name.
    /// </summary>
    /// <returns>List of properties name.</returns>
    public IEnumerable<string> Keys()
    {
        return _properties.Keys;
    }
        
    /// <summary>
    /// Getting the list of properties value.
    /// </summary>
    /// <returns>List of properties value.</returns>
    public IEnumerable<string> Values()
    {
        return _properties.Values;
    }
        
    /// <summary>
    /// Getting the list of properties key-value pair.
    /// </summary>
    /// <returns>List of properties key-value pair.</returns>
    public IEnumerable<KeyValuePair<string, string>> Items()
    {
        return _properties;
    }

    /// <summary>
    /// Returns a value whether the key exists.
    /// </summary>
    /// <param name="key">Property name.</param>
    /// <returns>Boolean value of key existence.</returns>
    public bool Contains(string key)
    {
        return _properties.ContainsKey(key);
    }
        
    /// <summary>
    /// Convert the current object to string.
    /// </summary>
    /// <returns>Converted string.</returns>
    public override string ToString()
    {
        return JsonSerializer.Serialize(_properties);
    }
}