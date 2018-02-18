using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using System.Xml;

namespace Ryan_UtilityCode.Dynamics.Configurations
{
    public static class ConfigFolder
    {
        /// <summary>
        /// Returns the name of the file 
        /// </summary>
        /// <param name="appName">Name of application to store configurations for</param>
        /// <param name="FileName">Specific file to store. If no extension is provided, .xml will be added</param>
        /// <returns></returns>
        public static string GetPath(string appName, string FileName)
        {
            if (!FileName.Contains('.'))
                FileName += ".xml";
            /*
            if(!appName.EndsWith("_Settings"))
                appName += "_Settings";
            
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(folder, appName, FileName); */
            return Path.Combine(GetFolder(appName, false), FileName);
        }
        public static string GetNetworkPath(string NetworkPath, string appName, string FileName)
        {
            if (!FileName.Contains('.'))
                FileName += ".xml";//default

            return Path.Combine(NetworkPath, appName, FileName);
        }
        /// <summary>
        /// Makes sure that the containing directory and subdirectories exist before returning the full file path
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="FileName">Name of actual file. If no extension is provided, .xml will be added</param>
        /// <returns></returns>
        public static string GetSafePath(string appName, string FileName)
        {
            if (!FileName.Contains('.'))
                FileName += ".xml";
            return Path.Combine(GetFolder(appName), FileName);
            /*
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if(!appName.EndsWith("_Settings"))
                appName += "_Settings";
            if (!FileName.Contains('.'))
                FileName += ".xml";
            folder = Path.Combine(folder, appName);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return Path.Combine(folder, FileName);*/
        }
        public static string GetFolder(string appName, bool safe = true)
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!appName.EndsWith("_Settings"))
                appName += "_Settings";
            folder = Path.Combine(folder, appName);
            if (safe && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }
        /// <summary>
        /// Overwrite the filepath with the xml content of the object
        /// </summary>
        /// <param name="toFile"></param>
        /// <param name="FilePath"></param>
        public static void SerializeToFile(this object toFile, string FilePath)
        {
            XmlSerializer xsr = new XmlSerializer(toFile.GetType());
            using(StreamWriter sw = new StreamWriter(FilePath, false))
            {
                xsr.Serialize(sw, toFile);
            }            
        }
        public static RT DeSerializeFile<RT>(string FilePath) //where RT:new()
        {
            XmlSerializer xsr = new XmlSerializer(typeof(RT));
            RT x;
            using(StreamReader sr = new StreamReader(FilePath))
            {
               x = (RT)xsr.Deserialize(sr);
            }
            return x;
        }
        /// <summary>
        /// Serialize the object to an XML string and return it
        /// </summary>
        /// <param name="toString"></param>
        /// <returns></returns>
        public static string SerializeToXML(this object toString)
        {
            XmlSerializer xsr = new XmlSerializer(toString.GetType());
            using (StringWriter sw = new StringWriter())
            {
                xsr.Serialize(sw, toString);
                return sw.ToString();
            }
        }
        /// <summary>
        /// Attempt to deserialize the XML into an object of type RT. Does not catch exceptions
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="XML"></param>
        /// <returns></returns>
        public static RT DeserializeXML<RT>(this string XML)
        {
            RT x;
            XmlSerializer xsr = new XmlSerializer(typeof(RT));
            using (StringReader sr = new StringReader(XML))
            {
                x = (RT)xsr.Deserialize(sr);
            }
            return x;
        }
    }
}
