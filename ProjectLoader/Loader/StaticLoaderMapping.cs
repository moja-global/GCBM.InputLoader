using System.Collections.Generic;

namespace Recliner2GCBM.Loader
{
    public class StaticLoaderMapping
    {
        public StaticLoaderMapping(string name,
                                   string table,
                                   IEnumerable<string> fields,
                                   params IEnumerable<object>[] data)
        {
            Name = name;
            Table = table;
            Fields = fields;
            Data = data;
        }

        public string Name { get; private set; }
        public string Table { get; private set; }
        public IEnumerable<string> Fields { get; private set; }
        public IEnumerable<IEnumerable<object>> Data { get; private set; }
    }
}
