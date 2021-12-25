using System.Diagnostics;
using System.IO;

namespace ExternalSorting;

public class FileReader
{
    readonly FileInfo fileInfo;
    
    public FileReader(string path)
    {
        this.fileInfo = new FileInfo(path);
    }

    /*public int CountChunks()
    {
        var totalFreeSpace = Process.GetCurrentProcess().PrivateMemorySize64 * 0.9; // I am very afraid swap so I will not use all memory;
        
    }*/
}