namespace Kentico.Xperience.Dynamics365.Sales.Constants
{
    /// <summary>
    /// Represents the type of a Dynamics 365 systemuser.
    /// </summary>
    /// <remarks>See <see href="https://docs.microsoft.com/en-us/dynamics365/customer-engagement/web-api/systemuser?view=dynamics-ce-odata-9#properties"/>.</remarks>
    public enum AccessModeEnum
    {
        ReadWrite,
        Administrative,
        Read,
        SupportUser,
        NonInteractive,
        DelegatedAdmin
    }
}