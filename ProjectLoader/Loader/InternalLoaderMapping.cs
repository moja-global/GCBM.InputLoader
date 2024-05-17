namespace Recliner2GCBM.Loader
{
    public class InternalLoaderMapping
    {
        public InternalLoaderMapping(string name, string sql)
        {
            Name = name;
            SQL = sql;
        }

        public string Name { get; private set; }
        public string SQL { get; private set; }
    }
}
