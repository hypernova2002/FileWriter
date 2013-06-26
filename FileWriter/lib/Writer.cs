using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FileWriter.lib
{
    /// <summary>
    /// Helper for creating files.
    /// </summary>
    /// <remarks>
    /// Currently only supports creating tsv's for simple types.
    /// </remarks>
    public static class Writer
    {
        static Hashtable _propertyCache;

        /// <summary>
        /// Initialises a new FileHelper with delimiter \t.
        /// </summary>
        static Writer()
        {
            _propertyCache = new Hashtable();
        }

        /// <summary>
        /// Turn flat generic enumerables into a byte array representation of a tsv.
        /// Complex types and arrays are currently not supported.
        /// </summary>
        /// <typeparam name="T">A parameter of any type</typeparam>
        /// <param name="Data">An enumerable of generic types</param>
        /// <returns>A byte array representation of a tsv</returns>
        public static byte[] DownloadBytes<T>(IEnumerable<T> Data)
        {
            return Encoding.UTF8.GetBytes(Download(Data));
        }

        /// <summary>
        /// Turn flat generic enumerables into a string representation of a tsv.
        /// Complex types and arrays are currently not supported.
        /// </summary>
        /// <typeparam name="T">A parameter of any type</typeparam>
        /// <param name="data">An enumerable of generic types</param>
        /// <returns>A string representation of a tsv</returns>
        public static string Download<T>(IEnumerable<T> data)
        {
            StringBuilder strdata = new StringBuilder();

            Type Ttype = typeof(T);
            DownloadProcessor FileProcessor = new DownloadProcessor();
            PropertyHelper helper = _propertyCache[Ttype.AssemblyQualifiedName] as PropertyHelper;

            if (helper == null)
            {
                helper = FileProcessor.GeneratePropertyHelper(Ttype);
                _propertyCache[Ttype.AssemblyQualifiedName] = helper;
            }

            return FileProcessor.ProcessObject(data, helper);
        }
    }

    public class DownloadProcessor
    {
        const string DEFAULT_FIELD_DELIMITER = "\t";
        const string DEFAULT_COLLECTION_DELIMITER = ",";

        string Delimiter;
        bool IsQuoted;
        StringBuilder FileResult;

        public DownloadProcessor()
            : this(DEFAULT_FIELD_DELIMITER)
        {
        }

        public DownloadProcessor(string delimiter)
        {
            this.Delimiter = delimiter;
            this.FileResult = new StringBuilder();
        }

        public string ProcessObject<T>(IEnumerable<T> data, PropertyHelper helper)
        {
            if (data == null) return String.Empty;

            GetHeader(helper);
            FileResult.Append(Environment.NewLine);

            foreach (T item in data)
            {
                try
                {
                    GetPropertyData(item, helper);
                    FileResult.Append(Environment.NewLine);
                }
                catch (Exception ex)
                {
                }
            }

            TrimFromEnd(Environment.NewLine);
            return FileResult.ToString();
        }

        public PropertyHelper GeneratePropertyHelper(Type type)
        {
            return FindProperties(type);
        }

        private void GetHeader(PropertyHelper helper)
        {
            if (helper == null) return;

            if (helper.NextProperties != null && helper.NextProperties.Count > 0)
                GetHeader(helper.NextProperties);

            if (helper.PropertyDetails == null) TrimFromEnd(Delimiter);
            else
            {
                if (!helper.ChildProperty)
                {
                    FileResult.Append(helper.FileDetails.Header).Append(Delimiter);
                }
            }
        }

        private void GetHeader(IEnumerable<PropertyHelper> helpers)
        {
            if (helpers == null) return;

            foreach (PropertyHelper helper in helpers)
                GetHeader(helper);
        }

        private void GetPropertyData<T>(T obj, PropertyHelper helper)
        {
            if (helper == null) return;

            if (helper.NextProperties != null && helper.NextProperties.Count > 0)
                GetPropertyData(obj, helper.NextProperties.ToArray());

            if (helper.PropertyDetails != null && !helper.ChildProperty)
                FileResult.Append(obj);
        }

        private void GetPropertyData<T>(T obj, IEnumerable<PropertyHelper> helpers)
        {
            if (helpers == null) return;

            foreach(PropertyHelper helper in helpers)
            {
                IEnumerable objCol = obj as IEnumerable;

                if (objCol != null)
                {
                    string CollectionDelimiter = helper.FileDetails.Delimiter ?? DEFAULT_COLLECTION_DELIMITER;
                    
                    foreach (var item in objCol)
                    {
                        GetPropertyData(helper.PropertyDetails.GetValue(item, null), helper);
                        FileResult.Append(CollectionDelimiter);
                    }

                    TrimFromEnd(CollectionDelimiter);
                }
                else
                    GetPropertyData(helper.PropertyDetails.GetValue(obj, null), helper);

                FileResult.Append(Delimiter);
            }

            TrimFromEnd(Delimiter);
        }

        private void TrimFromEnd(string trimmer)
        {
            if (FileResult.Length > trimmer.Length) FileResult.Remove(FileResult.Length - trimmer.Length, trimmer.Length);
        }

        private PropertyHelper FindProperties(Type T)
        {
            return FindProperties(T, new PropertyHelper());
        }

        private PropertyHelper FindProperties(Type T, PropertyHelper helper)
        {
            if (T == null) return helper;
            if (helper == null) return helper;
            if (helper.NextProperties == null) helper.NextProperties = new List<PropertyHelper>();

            PropertyInfo[] properties = T.GetProperties();

            foreach (PropertyInfo p in properties)
            {
                FileAttribute attr = GetPropertyName(p);
                if (attr == null) continue;

                PropertyHelper NextHelper = new PropertyHelper(p, attr);

                if (PropertyIsEnumerable(p))
                    NextHelper = FindProperties(p.PropertyType.GenericTypeArguments.First(), NextHelper);
                else
                    NextHelper = FindProperties(p.PropertyType, NextHelper);

                NextHelper.ChildProperty = attr.IncludeChildren;

                helper.NextProperties.Add(NextHelper);
            }

            return helper;
        }

        private bool PropertyIsEnumerable(PropertyInfo property)
        {
            return property.PropertyType.GetInterface("IEnumerable") != null && property.PropertyType != typeof(string);
        }

        private FileAttribute GetPropertyName(PropertyInfo property)
        {
            return GetFileAttribute(property);
        }

        private FileAttribute GetFileAttribute(PropertyInfo property)
        {
            return property.GetCustomAttribute(typeof(FileAttribute), true) as FileAttribute;
        }
    }

    class PropertyHelper
    {
        public PropertyInfo PropertyDetails { get; set; }
        public FileAttribute FileDetails { get; set; }
        public bool ChildProperty { get; set; }
        public List<PropertyHelper> NextProperties { get; set; }

        public PropertyHelper()
            : this(null, null)
        {
        }

        public PropertyHelper(PropertyInfo pinfo, FileAttribute filedetail)
            : this(pinfo, filedetail, false)
        {
        }

        public PropertyHelper(PropertyInfo pinfo, FileAttribute filedetail, bool childproperty)
        {
            this.PropertyDetails = pinfo;
            this.FileDetails = filedetail;
            this.ChildProperty = childproperty;
        }
    }
}