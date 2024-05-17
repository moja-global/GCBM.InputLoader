using System;
using Recliner2GCBM.ViewModel.Support;

namespace Recliner2GCBM.Configuration
{
    public class ProjectType : BindableBase, IEquatable<ProjectType>
    {
        private ProjectMode projectMode;
        private ModuleConfiguration moduleConfiguration;

        public ProjectType(ProjectMode projectMode, ModuleConfiguration moduleConfiguration)
        {
            this.projectMode = projectMode;
            this.moduleConfiguration = moduleConfiguration;
        }

        public ProjectMode Mode
        {
            get => projectMode;
            set => SetProperty(ref projectMode, value);
        }

        public ModuleConfiguration Configuration
        {
            get => moduleConfiguration;
            set => SetProperty(ref moduleConfiguration, value);
        }

        public bool Equals(ProjectType other)
        {
            return Mode == other.Mode
                && Configuration == other.Configuration;
        }

        public override bool Equals(object obj)
        {
            return obj is ProjectType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return 17 +
                3 * Mode.GetHashCode() +
                3 * Configuration.GetHashCode();
        }
    }
}
