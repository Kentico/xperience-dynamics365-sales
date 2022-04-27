namespace Kentico.Xperience.Dynamics365.Sales.Constants
{
    /// <summary>
    /// Available values for a party's participation type when creating parties for activities.
    /// </summary>
    /// <remarks>See <see href="https://docs.microsoft.com/en-us/dynamics365/customer-engagement/web-api/activityparty?view=dynamics-ce-odata-9"/>.</remarks>
    public enum ParticipationTypeMaskEnum
    {
        None,
        Sender,
        ToRecipient,
        CCRecipient,
        BCCRecipient,
        RequiredAttendee,
        OptionalAttendee,
        Organizer,
        Regarding,
        Owner,
        Resource,
        Customer
    }
}