using System;
using System.Collections.Generic;

namespace Recliner2GCBM.Loader
{
    public class ValueDatasourceLoaderMapping
    {
        public ValueDatasourceLoaderMapping(
            string name,
            string loadSql,
            params Tuple<string, int>[] parameterMappings)
        {
            Name = name;
            LoadSQL = loadSql;
            ParameterMappings = parameterMappings;
        }

        public string Name { get; private set; }
        public string LoadSQL { get; private set; }
        public IList<Tuple<string, int>> ParameterMappings { get; private set; }
    }
}
