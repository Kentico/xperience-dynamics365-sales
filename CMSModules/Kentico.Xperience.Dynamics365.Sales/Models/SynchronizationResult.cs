using System.Collections.Generic;

namespace Kentico.Xperience.Dynamics365.Sales.Models
{
    /// <summary>
    /// The result of a synchronization process to or from Dynamics 365.
    /// </summary>
    public class SynchronizationResult
    {
        /// <summary>
        /// True if the synchronization process encountered errors.
        /// </summary>
        public bool HasErrors
        {
            get;
            set;
        }


        /// <summary>
        /// A list of error messages encountered during the synchronization process.
        /// </summary>
        public IEnumerable<string> SynchronizationErrors
        {
            get;
            set;
        }


        /// <summary>
        /// The total number of objects that were successfully synchronized.
        /// </summary>
        public int SynchronizedObjectCount
        {
            get;
            set;
        }


        /// <summary>
        /// A list of human-friendly identifiers for the objects that were not synchronized
        /// due to errors during the process.
        /// </summary>
        public IEnumerable<string> UnsynchronizedObjectIdentifiers
        {
            get;
            set;
        }
    }
}