using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ExcelDataReader;

namespace Recliner2GCBM.Loader.Datasource
{
    public class XLSXDatasource : ValueDatasource
    {
        private string path;
        private int worksheet;
        private bool header;

        private int firstDataRow = 1;
        private int lastDataRow = -1;

        public XLSXDatasource(string path, int worksheet, bool header = false)
        {
            this.path = path;
            this.worksheet = worksheet;
            this.header = header;
        }

        public IEnumerable<IList<string>> Read()
        {
            using (var reader = Open())
            {
                var workbook = reader.AsDataSet();
                var sheet = workbook.Tables[worksheet];
                DetectRangeStart(sheet);
                DetectRangeEnd(sheet);
                for (int rowNum = firstDataRow; rowNum <= lastDataRow; rowNum++)
                {
                    yield return ReadRow(sheet, rowNum);
                }
            }
        }

        private IExcelDataReader Open()
        {
            if (!File.Exists(path))
            {
                throw new IOException($"File not found: {path}.");
            }

            var stream = File.Open(path, FileMode.Open, FileAccess.Read);
            return ExcelReaderFactory.CreateReader(stream);
        }
        
        private void DetectRangeStart(DataTable sheet)
        {
            for (int rowNum = 0; rowNum < sheet.Rows.Count; rowNum++)
            {
                var row = sheet.Rows[rowNum];
                for (int colNum = 0; colNum < sheet.Columns.Count; colNum++)
                {
                    var cell = sheet.Rows[rowNum][colNum];
                    var value = cell.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        firstDataRow = rowNum;
                        if (header)
                        {
                            firstDataRow++;
                        }

                        return;
                    }
                }
            }
        }

        private void DetectRangeEnd(DataTable sheet)
        {
            for (int rowNum = sheet.Rows.Count - 1; rowNum >= firstDataRow; rowNum--)
            {
                var row = sheet.Rows[rowNum];
                for (int colNum = 0; colNum < sheet.Columns.Count; colNum++)
                {
                    var cell = sheet.Rows[rowNum][colNum];
                    var value = cell.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        lastDataRow = rowNum;
                        return;
                    }
                }
            }
        }
        
        private IList<string> ReadRow(DataTable sheet, int rowNum)
        {
            return (
                from cell
                in sheet.Rows[rowNum].ItemArray
                select cell.ToString()
            ).ToList();
        }
    }
}
