using System.IO;
using System.Windows;
using unvell.ReoGrid;
using unvell.ReoGrid.IO;

namespace Recliner2GCBM.ViewModel.Support
{
    public class GridHelper
    {
        public static void LoadFile(ReoGridControl grid, string path)
        {
            string ext = Path.GetExtension(path);
            try
            {
                switch (ext)
                {
                    case ".xlsx":
                        grid.Load(path, FileFormat.Excel2007);
                        break;
                    case ".csv":
                        grid.Load(path, FileFormat.CSV);
                        break;
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static void Dispose(ReoGridControl grid)
        {
            if (grid != null)
            {
                grid.CurrentWorksheet.Reset();
                grid.Dispose();
            }
        }
    }
}
