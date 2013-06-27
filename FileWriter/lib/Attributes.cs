using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileWriter.lib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class FileAttribute : Attribute
    {
        public string Header;
        public string Delimiter;
        public bool IncludeChildren;

        public FileAttribute()
        {
        }

        public FileAttribute(string header)
        {
            this.Header = header;
        }
    }
}
