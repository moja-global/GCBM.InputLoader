using Recliner2GCBM.Loader.Datasource;

namespace Recliner2GCBM.Loader
{
    public class ClassifierLoaderMapping
    {
        public ClassifierLoaderMapping(string name,
                                       string classifierName,
                                       int column,
                                       ValueDatasource datasource)
        {
            Name = name;
            ClassifierName = classifierName;
            Column = column;
            Datasource = datasource;
        }

        public string Name { get; private set; }
        public string ClassifierName { get; private set; }
        public int Column { get; private set; }
        public ValueDatasource Datasource { get; private set; }
    }
}
