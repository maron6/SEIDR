using SEIDR.OperationServiceModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.FileSystem
{
    public class MD : iOperationMetaData
    {
        public string Description
        {
            get
            {
                return @"Operation for performing FileSystem Operations:
MOVE (includes a name change, replace * with current Name), COPY, 
MOVEDIR (just move to other directory), COPYDIR,
CREATEDIR (Ensure directory exists),
CHECK/EXISTS (Ensure file exists),
GRAB (Checks for the file and adds it to the batch),
GRABALL (Checks for any files in the folder matching mask and adds to the batch),
TAG (Copy file to destination location, but also tags the end of the file with the file's name to change hash)";
            }
        }

        public string Operation
        {
            get
            {
                return "FileSystem";
            }
        }

        public string OperationSchema
        {
            get
            {
                return "SEIDR";
            }
        }

        public string ParameterSelect
        {
            get
            {
                return "usp_SEIDR_FS_Parameter_SL";
            }
        }

        public byte? ThreadID
        {
            get
            {
                return null;
            }
        }

        public int Version
        {
            get
            {
                return 1;
            }
        }
    }
}
