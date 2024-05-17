using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;

namespace Recliner2GCBM.Loader.Datasource
{
    public class CSVDatasource : ValueDatasource
    {
        private string path;
        private bool header;

        public CSVDatasource(string path, bool header = false)
        {
            this.path = path;
            this.header = header;
        }

        public IEnumerable<IList<string>> Read()
        {
            using (var reader = new TextFieldParser(path))
            {
                reader.TextFieldType = FieldType.Delimited;
                reader.SetDelimiters(",");

                if (header)
                {
                    reader.ReadFields();
                }

                while (!reader.EndOfData)
                {
                    yield return reader.ReadFields();
                }
            }
        }
    }
}
