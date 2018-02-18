using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using System.Xml;

namespace SEIDR.Dynamics.Configurations
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
        /// <summary>
        /// Gets the file path for the app/fileName in the directory specified by NetworkPath
        /// </summary>
        /// <param name="NetworkPath"></param>
        /// <param name="appName"></param>
        /// <param name="FileName">Default extension of .xml if there is none</param>
        /// <returns></returns>
        public static string GetNetworkPath(string NetworkPath, string appName, string FileName)
        {
            if (!FileName.Contains('.'))
                FileName += ".xml";//default

            return Path.Combine(NetworkPath, appName, FileName);
        }
        /// <summary>
        /// Returns the network file path for the app/subfolder within the Network path
        /// </summary>
        /// <param name="NetworkPath"></param>
        /// <param name="appName"></param>
        /// <param name="SubFolder"></param>
        /// <param name="FileName">Default extension of .xml if there is none</param>
        /// <returns></returns>
        public static string GetNetworkSubPath(string NetworkPath, string appName, string SubFolder, string FileName)
        {
            if (!FileName.Contains('.'))
                FileName += ".xml";

            return Path.Combine(NetworkPath, appName, SubFolder, FileName);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="NetworkPath"></param>
        /// <param name="appName"></param>
        /// <param name="SubFolder"></param>
        /// <param name="FileName">Default extension of .xml if there is none</param>
        /// <returns>Full file path</returns>
        public static string GetSafeNetworkSubPath(string NetworkPath, string appName, string SubFolder, string FileName)
        {
            if (!FileName.Contains('.'))
                FileName += ".xml";
            string Folder = Path.Combine(NetworkPath, appName, SubFolder);
            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);
            return Path.Combine(Folder, FileName);
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
            //if (!appName.EndsWith("_Settings"))
            //    appName += "_Settings";
            folder = Path.Combine(folder, appName);
            if (safe && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }
        public static string GetSafePath(string appName, string subFolder, string FileName)
        {
            if (!FileName.Contains('.'))
                FileName += ".xml";
            string Folder = Path.Combine(GetFolder(appName), subFolder);
            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);
            return Path.Combine(Folder, FileName);
        }
        public static string GetFolder(string appName, string SubFolder, bool safe = true)
            => GetFolder(Path.Combine(appName, SubFolder), safe);
        /// <summary>
        /// Overwrite the filepath with the xml content of the object
        /// </summary>
        /// <param name="toFile"></param>
        /// <param name="FilePath"></param>
        public static void SerializeToFile<ST>(this /*object*/ ST toFile, string FilePath)
        {
            XmlSerializer xsr = new XmlSerializer(/*toFile.GetType()*/ typeof(ST));
            using(StreamWriter sw = new StreamWriter(FilePath, false))
            {
                xsr.Serialize(sw, toFile);
            }            
        }
        /// <summary>
        /// Deserialize the file's content into an instance fo type RT
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="FilePath"></param>
        /// <returns></returns>
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
        public static string SerializeToXML<RT>(this RT toString) //(this object toString)
        {
            XmlSerializer xsr = new XmlSerializer(typeof(RT));
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
        public static void DeserializeXML<RT>(this string XML, ref RT result)
        {
            XmlSerializer xsr = new XmlSerializer(result.GetType());
            using(StringReader sr = new StringReader(XML))
            {
                result = (RT)xsr.Deserialize(sr);
            }
        }
    }
}
