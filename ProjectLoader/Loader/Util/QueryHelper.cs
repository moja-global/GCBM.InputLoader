using System.Collections.Generic;
using System.Linq;

namespace Recliner2GCBM.Loader.Util
{
    public static class QueryHelper
    {
        public static IList<string> ExtractParameters(string sql)
        {
            var paramSegments = sql.Split('@');
            if (paramSegments.Count() == 1)
            {
                return new List<string>();
            }

            var parameters = new HashSet<string>();
            foreach (var segment in paramSegments.Skip(1))
            {
                var paramEnd = segment.IndexOfAny(new char[] {
                    ',', ' ', ')', '\r', '\n'
                });

                parameters.Add(paramEnd == -1
                    ? segment
                    : segment.Substring(0, paramEnd));
            }

            return parameters.ToList();
        }
    }
}
