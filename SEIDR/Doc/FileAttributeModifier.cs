using System;
using System.IO;

namespace SEIDR.Doc
{
    /// <summary>
    /// Use to change the attributes of a given File.
    /// </summary>
    public static class FAttModder
    {
        /// <summary>
        /// Removes any of the listed attributes from the file.
        /// <para>Throws an exception if the file does not exist.</para>
        /// </summary>
        /// <param name="fullFilePath">Full file path for file we want to change attributes on.</param>
        /// <param name="toRemove">List of FileAttributes to be turned off.</param>
        public static void RemoveAttribute(string fullFilePath, params FileAttributes[] toRemove)
        {
            if (!File.Exists(fullFilePath))
                throw new Exception("File does not exist.");
            FileAttributes f = File.GetAttributes(fullFilePath);
            foreach (var fa in toRemove)
            {
                f = f & ~fa; //  0000 0010 -> 1111 1101, so only flagged bit is forced to go to zero
            }
            File.SetAttributes(fullFilePath, f);            
        }
        /// <summary>
        /// Adds any of the listed attributes from the file.
        /// <para>Throws an exception if the file does not exist.</para>
        /// </summary>
        /// <param name="fullFilePath">Full file path for file we want to change attributes on.</param>
        /// <param name="toAdd">List of FileAttributes to be turned on.</param>
        public static void AddAttribute(string fullFilePath, params FileAttributes[] toAdd)
        {
            if (!File.Exists(fullFilePath))
                throw new Exception("File does not exist.");
            FileAttributes f = File.GetAttributes(fullFilePath);
            foreach (var fa in toAdd)
            {
                f = f | fa;
            }
            File.SetAttributes(fullFilePath, f);
        }
    }
}
