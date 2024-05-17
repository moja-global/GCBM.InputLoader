namespace Recliner2GCBM.Loader
{
    public class SQLLoaderMapping
    {
        public SQLLoaderMapping(string name,
                                string fetchSql,
                                string loadSql)
        {
            Name = name;
            FetchSQL = fetchSql;
            LoadSQL = loadSql;
        }

        public string Name { get; private set; }
        public string FetchSQL { get; private set; }
        public string LoadSQL { get; private set; }
    }
}
