using System;

namespace Kentico.Xperience.Dynamics365.Sales.Models.Activities
{
    /// <summary>
    /// Represents the data for Dynamics 365 appointment creation.
    /// </summary>
    public class Dynamics365AppointmentModel
    {
        /// <summary>
        /// The subject of the appointment.
        /// </summary>
        public string Subject
        {
            get;
            set;
        }


        /// <summary>
        /// The description of the appointment.
        /// </summary>
        public string Description
        {
            get;
            set;
        }


        /// <summary>
        /// The appointment's required attendee in the format <i>/systemusers(id)</i>.
        /// </summary>
        public string RequiredAttendee
        {
            get;
            set;
        }


        /// <summary>
        /// The appointment's optional attendee in the format <i>/systemusers(id)</i>.
        /// </summary>
        public string OptionalAttendee
        {
            get;
            set;
        }


        /// <summary>
        /// The location of the appointment.
        /// </summary>
        public string Location
        {
            get;
            set;
        }


        /// <summary>
        /// True if the task lasts the entirety of the <see cref="StartTime"/> day.
        /// </summary>
        public bool IsAllDay
        {
            get;
            set;
        }


        /// <summary>
        /// The start time of the appointment.
        /// </summary>
        public DateTime StartTime
        {
            get;
            set;
        } 


        /// <summary>
        /// The end time of the appointment.
        /// </summary>
        public DateTime EndTime
        {
            get;
            set;
        }
    }
}