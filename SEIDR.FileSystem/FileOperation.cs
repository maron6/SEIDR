using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.FileSystem
{
    //ToDo: Add Compress, decompress
    public enum FileOperation
    {
        TAG = 1,
        CHECK = 2,
        EXIST = 3,
        COPY = 4,
        MOVE = 5,
        COPYDIR = 6,
        MOVEDIR = 7,
        GRAB = 8,
        GRAB_ALL = 9,
        CREATEDIR = 10,
        DELETE = 11,
        CREATE_DUMMY = 12
    }
}
