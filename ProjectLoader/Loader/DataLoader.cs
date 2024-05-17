using System.Collections.Generic;
using System.Data;

namespace Recliner2GCBM.Loader
{
    public interface DataLoader
    {
        int Count();
        IEnumerable<string> Load(IDbConnection outputDb);
    }
}
