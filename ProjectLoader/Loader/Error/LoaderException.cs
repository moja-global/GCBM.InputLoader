using System;

namespace Recliner2GCBM.Loader.Error
{
    public class LoaderException : Exception
    {
        public LoaderException() { }
        public LoaderException(string message) : base(message) { }
        public LoaderException(string message, Exception inner) : base(message, inner) { }

        public LoaderException(string loaderName, string message) : base(message)
        {
            LoaderName = loaderName;
        }

        public string LoaderName { get; private set; }
    }
}
