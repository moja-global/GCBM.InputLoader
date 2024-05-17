using System.Collections.Generic;

namespace Recliner2GCBM.Loader.Datasource
{
    public class ListDatasource : ValueDatasource
    {
        IEnumerable<IList<string>> values;

        public ListDatasource(IEnumerable<IList<string>> values)
        {
            this.values = values;
        }

        public IEnumerable<IList<string>> Read()
        {
            foreach (var value in values)
            {
                yield return value;
            }
        }
    }
}
