using Kentico.Xperience.Dynamics365.Sales.Services;

namespace Kentico.Xperience.Dynamics365.Sales.Models.Activities
{
    /// <summary>
    /// Represents the data for Dynamics 365 task creation.
    /// </summary>
    public class Dynamics365TaskModel
    {
        /// <summary>
        /// The subject of the task.
        /// </summary>
        public string Subject
        {
            get;
            set;
        }


        /// <summary>
        /// The description of the task.
        /// </summary>
        public string Description
        {
            get;
            set;
        }


        /// <summary>
        /// The amount of hours from task creation that it is due.
        /// </summary>
        public int DueInHours
        {
            get;
            set;
        }


        /// <summary>
        /// The amount of minutes the task should take to complete.
        /// </summary>
        public int DurationMinutes
        {
            get;
            set;
        }


        /// <summary>
        /// The task priority. Use <see cref="IDynamics365Client.GetOptionSet"/> to find the allowed values.
        /// </summary>
        public int Priority
        {
            get;
            set;
        }
    }
}