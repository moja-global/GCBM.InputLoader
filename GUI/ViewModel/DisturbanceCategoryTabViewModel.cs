using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.OleDb;
using System.Data.SQLite;
using System.Windows.Input;
using Recliner2GCBM.ViewModel.Support;

namespace Recliner2GCBM.ViewModel
{
    public class DisturbanceCategoryTabViewModel : BindableBase
    {
        public DisturbanceCategoryTabViewModel(ApplicationContext applicationContext)
        {
            AppContext = applicationContext;
            AppContext.ProjectConfiguration.PropertyChanged += OnAIDBPathChanged;
            AppContext.PropertyChanged += OnProjectConfigurationChanged;
        }

        public ApplicationContext AppContext { get; private set; }

        public ICommand SetNaturalCategoryCommand => new DelegateCommand(SetNaturalCategory);
        public ICommand SetAnthropogenicCategoryCommand => new DelegateCommand(SetAnthropogenicCategory);
        public ICommand LeftKeyCommand => new DelegateCommand(SetNaturalCategory);
        public ICommand RightKeyCommand => new DelegateCommand(SetAnthropogenicCategory);
        public ICommand BackCommand => new DelegateCommand(Back);
        public ICommand NextCommand => new DelegateCommand(Next);

        private void OnAIDBPathChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AIDBPath")
            {
                RefreshDisturbanceList();
            }
        }

        private void OnProjectConfigurationChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ProjectConfiguration")
            {
                AppContext.ProjectConfiguration.PropertyChanged += OnAIDBPathChanged;
                RefreshDisturbanceList();
            }
        }

        private void RefreshDisturbanceList()
        {
            var aidbPath = AppContext.ProjectConfiguration.AIDBPath;
            if (aidbPath.EndsWith(".mdb"))
            {
                var aidbDistTypes = GetAccessDistTypes(aidbPath);
                AppContext.ProjectConfiguration.RefreshDisturbances(aidbDistTypes);
            }
            else if (aidbPath.EndsWith(".db"))
            {
                var aidbDistTypes = GetSQLiteDistTypes(aidbPath);
                AppContext.ProjectConfiguration.RefreshDisturbances(aidbDistTypes);
            }
        }

        private IEnumerable<string> GetAccessDistTypes(string path)
        {
            string provider = Environment.Is64BitProcess
                ? "Microsoft.ACE.OLEDB.12.0"
                : "Microsoft.Jet.OLEDB.4.0";

            var connectionString = $"Provider={provider};Data Source={path};";
            var aidb = new OleDbConnection(connectionString);
            aidb.Open();

            try
            {
                var disturbanceTypes = new List<string>();
                using (var command = new OleDbCommand("SELECT disttypename FROM tbldisturbancetypedefault", aidb))
                {
                    var results = command.ExecuteReader();
                    while (results.Read())
                    {
                        disturbanceTypes.Add(results.GetString(0));
                    }
                }

                return disturbanceTypes;
            }
            finally
            {
                aidb.Close();
            }
        }

        private IEnumerable<string> GetSQLiteDistTypes(string path)
        {
            var aidb = new SQLiteConnection($"Data Source={path};Version=3;");
            aidb.Open();

            try
            {
                var disturbanceTypes = new List<string>();
                using (var command = aidb.CreateCommand())
                {
                    command.CommandText = "SELECT name FROM disturbance_type";
                    var results = command.ExecuteReader();
                    while (results.Read())
                    {
                        disturbanceTypes.Add(results.GetString(0));
                    }
                }

                return disturbanceTypes;
            }
            finally
            {
                aidb.Close();
            }
        }

        private void SetNaturalCategory(object parameter)
        {
            if (parameter == null)
            {
                return;
            }

            var row = (Tuple<string, string>)parameter;
            var configItemIndex = AppContext.ProjectConfiguration.DisturbanceTypeCategories.IndexOf(row);
            AppContext.ProjectConfiguration.DisturbanceTypeCategories[configItemIndex] =
                new Tuple<string, string>(row.Item1, "N");
        }

        private void SetAnthropogenicCategory(object parameter)
        {
            if (parameter == null)
            {
                return;
            }

            var row = (Tuple<string, string>)parameter;
            var configItemIndex = AppContext.ProjectConfiguration.DisturbanceTypeCategories.IndexOf(row);
            AppContext.ProjectConfiguration.DisturbanceTypeCategories[configItemIndex] =
                new Tuple<string, string>(row.Item1, "A");
        }

        private void Back() => AppContext.SelectedTabIndex--;
        private void Next() => AppContext.SelectedTabIndex++;
    }
}
