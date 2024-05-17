using System.Collections.ObjectModel;
using Recliner2GCBM.ViewModel.Support;
using System.Linq;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Recliner2GCBM.Loader.Util;

namespace Recliner2GCBM.Configuration
{
    public class ProjectConfiguration : BindableBase
    {
        private ProjectType projectType;
        private string aidbPath;
        private ProviderConfiguration outputConfiguration;
        private ObservableCollection<Tuple<string, string>> disturbanceTypeCategories =
            new ObservableCollection<Tuple<string, string>>();

        public ProjectConfiguration()
        {
            projectType = new ProjectType(ProjectMode.SpatiallyExplicit, ModuleConfiguration.CBMClassic);
            outputConfiguration = new ProviderConfiguration("SQLite");
            ClassifierSet = new ObservableCollection<ClassifierConfiguration>();
            GrowthCurves = new GrowthCurveConfiguration();
            TransitionRules = new TransitionRuleConfiguration();
        }

        public ProjectType Project
        {
            get => projectType;
            set => SetProperty(ref projectType, value);
        }

        public ProviderConfiguration OutputConfiguration
        {
            get => outputConfiguration;
            set => SetProperty(ref outputConfiguration, value);
        }

        public string AIDBPath
        {
            get => aidbPath;
            set => SetProperty(ref aidbPath, value);
        }

        [JsonConverter(typeof(TupleJsonConverter))]
        public ObservableCollection<Tuple<string, string>> DisturbanceTypeCategories
        {
            get => disturbanceTypeCategories;
            set => SetProperty(ref disturbanceTypeCategories, value);
        }

        public ObservableCollection<ClassifierConfiguration> ClassifierSet { get; private set; }
        public GrowthCurveConfiguration GrowthCurves { get; private set; }
        public TransitionRuleConfiguration TransitionRules { get; private set; }

        public void RefreshDisturbances(IEnumerable<string> disturbanceTypes)
        {
            // Preserve any existing category selections and add any new disturbance
            // types with a default category of "A".
            DisturbanceTypeCategories = new ObservableCollection<Tuple<string, string>>(((
                from item in disturbanceTypeCategories
                where disturbanceTypes.Contains(item.Item1)
                select item
            ).Concat((
                from distType in disturbanceTypes
                where (from item in disturbanceTypeCategories where item.Item1 == distType select item).Count() == 0
                select new Tuple<string, string>(distType, "A")
            ))).ToList());
        }

        public void RefreshClassifiers()
        {
            GrowthCurves.RefreshClassifiers(from classifier in ClassifierSet select classifier.Name);
            TransitionRules.RefreshClassifiers(from classifier in ClassifierSet select classifier.Name);
        }
    }
}
