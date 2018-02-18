using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dts.Runtime;
using Ryan_UtilityCode.Dynamics.Windows;

namespace SEIDR_ProfileManager
{
    public static class DTS_SaveHelper
    {
        //TODO: Migrate DTS package saving/upload to addon...
        public static void SaveDTSPackage(string fullPath, string packName, string IntegrationServiceServer, string packageFolder = null)
        {
            string IS_Server = IntegrationServiceServer;
            if (System.IO.File.Exists(fullPath))
            {
                string d = fullPath.Substring(0, fullPath.LastIndexOf('.'));
                System.IO.FileInfo f = new System.IO.FileInfo(fullPath);
                System.IO.File.Move(fullPath, d + f.CreationTime.ToString("_yyyyMMdd") + ".dtsx");
            }
            //string ft = System.IO.Path.GetFileName(fullPath);
            //fullPath = fullPath.Substring(fullPath.IndexOf("C:\\") + 3);
            Application app = new Application();
            Package p = app.LoadFromSqlServer(packName, IS_Server, null, null, null);

            app.SaveToXml(fullPath, p, null);
        }

        /// <summary>
        /// Upload a package to the server specified in MiscSetting
        /// </summary>
        /// <param name="PackageName">Save location. Combines with \MetrixPreprocessing\ unless packageFolder is provided</param>
        /// <param name="PackagePath">File System path to package to save</param>
        /// <param name="ms">Contains settings</param>
        /// <param name="packageFolder"></param>
        /// <returns>True if updated, else false</returns>
        public static bool UploadDTSPackage(string PackageName, string PackagePath, string IntegrationServiceServer, string packageFolder = null)
        {
            string IS_Server = IntegrationServiceServer;
            if (PackageName == null || PackagePath == null)
                return false;

            string packFullName = @packageFolder ?? @"\MetrixPreProcessing\" + PackageName;
            Microsoft.SqlServer.Dts.Runtime.Application app = new Microsoft.SqlServer.Dts.Runtime.Application();
            if (app.ExistsOnSqlServer(packFullName, IS_Server, null, null))
            {
                //OverWriteConfirm owc = new OverWriteConfirm("Really OverWrite Package \"" + PackageName + "\" on "+ IS_Server +" Integration Services?");
                var r = new Alert("Overwrite Package \"" + packFullName + "\" on " + IS_Server + " Integration Services?").ShowDialog();
                //owc.ShowDialog();
                if (!r.HasValue || !r.Value)
                {
                    return false;
                }
            }
            Package toSave = app.LoadPackage(PackagePath, null);
            app.SaveToSqlServerAs(toSave, null, packFullName, IS_Server, null, null);
            return true;
        }
    }
}
