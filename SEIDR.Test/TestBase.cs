using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SEIDR.Test
{
    public class TestBase
    {
        public DirectoryInfo RootDirectory { get; set; } = null;
        const string TEST_DIRECTORY = @"C:\SEIDR\";
        public FileInfo GetFile(params string[] TestProjectFile)
        {
            var p = new string[TestProjectFile.Length + 1];
            p[0] = @"..\..\";
            for(int i = 0; i < TestProjectFile.Length; i++)
            {
                p[i + 1] = TestProjectFile[i];
            }
            FileInfo orig = new FileInfo(Path.Combine(p));
            string Dest = Path.Combine(TEST_DIRECTORY, orig.Name);
            File.Copy(orig.FullName, Dest, true);
            return new FileInfo(Dest);
        }
        public DirectoryInfo PrepDirectory(bool clean)
        {
            DirectoryInfo di = new DirectoryInfo(TEST_DIRECTORY);
            if (!di.Exists)
                di.Create();
            else if(clean)
            {
                var fl = di.EnumerateFiles("*.*", SearchOption.AllDirectories);
                fl.ForEach(f => f.Delete());
                try
                {
                    var dl = di.EnumerateDirectories("*", SearchOption.AllDirectories);
                    dl.ForEach(d => d.Delete());
                }
                catch
                {

                }                
            }
            RootDirectory = di;
            return di;
        }
    }
}
