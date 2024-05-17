using System.Collections.Generic;

namespace Recliner2GCBM.Loader.Datasource
{
    public interface ValueDatasource
    {
        IEnumerable<IList<string>> Read();
    }
}
