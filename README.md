[![Stack Overflow](https://img.shields.io/badge/Stack%20Overflow-ASK%20NOW-FE7A16.svg?logo=stackoverflow&logoColor=white)](https://stackoverflow.com/tags/kentico) ![Kentico.Xperience.Libraries 13.0.0](https://img.shields.io/badge/Kentico.Xperience.Libraries-v13.0.0-orange)

# Xperience Dynamics 365 Sales integration

This integration enables the synchronization of Xperience contacts and activities to a Dynamics 365 tenant, and synchronizing updates of those contacts in Dynamics 365 back to your Xperience website. It also contains custom Marketing automation actions to log Dynamics 365 tasks and appointments.

## Set up the environment

### Import the custom module

1. Download the latest export package from the [/CMSSiteUtils/Export](/CMSSiteUtils/Export) folder.
1. In the Xperience adminstration, open the __Sites__ application.
1. [Import](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects) the downloaded package with the __Import files__ and __Import code files__ [settings](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects#Importingasiteorobjects-Import-Objectselectionsettings) enabled.
1. Perform the [necessary steps](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects#Importingasiteorobjects-Importingpackageswithfiles) to include the following imported folder in your project:
   - `/CMSModules/Kentico.Xperience.Dynamics365.Sales`

### Enable the integration

1. Open the _web.config_ of your Xperience CMS project and navigate to the `<appSettings>` section.
2. Add your Dynamics 365 Sales tenant to the __Dynamics365URL__ application setting. This is the URL where you access Dynamics 365. If you don't have  an instance, you can set up a new one using [Microsoft's documentation](https://docs.microsoft.com/en-us/dynamics365/sales/set-up-dynamics-365-sales).

```xml
<add key="Dynamics365URL" value="https://mycompany.crm4.dynamics.com" />
```

3. To authenticate requests to Dynamics 365, [register an application](https://docs.microsoft.com/en-us/previous-versions/dynamicscrm-2016/developers-guide/mt622431(v=crm.8)#register-an-application-with-microsoft-azure) in Microsoft Azure Active Directory's __App registrations__ tab.
4. After registration, select the application registration, note the __Application (client) ID__ and __Directory (tenant) ID__ in the Azure Portal, and set the corresponding application setting:

```xml
<add key="Dynamics365ClientID" value="<client ID>" />
<add key="Dynamics365TenantID" value="<tenant ID>" />
```

5. Click the __Certificates & secrets__ tab, navigate to the __Client secrets__ tab, and create a new secret for the integration. Add this secret to the __Dynamics365Secret__ application setting:

```xml
<add key="Dynamics365Secret" value="<your secret>" />
```

### Enable outgoing contact synchronization

Outgoing contact synchronization is performed by the scheduled task "Dynamics 365 contact synchronization" which is set to run every hour by default. However, you must first enable the synchronization by going to the __Settings__ application, navigating to the __Integration → Dynamics 365__ section, and checking the __Enabled__ box under the "Contact synchronization" category.

The scheduled task will only synchronize contacts that have reached a minimum total score. Ensure that you have the [contact scoring](https://docs.xperience.io/on-line-marketing-features/managing-your-on-line-marketing-features/contact-management/scoring-contacts) functionality configured properly, then set the minimum score in the __Minimum score__ setting. The score must be greater than zero.

This integration uses two custom contact fields to store information about contact synchronization, which must be created manually:

1. In the Xperience administration, open the __Modules__ application.
1. Edit the __Contact management__ module → __Classes__ tab __→ Contact__ class __→ Fields__ tab.
1. Add two __New fields__ with the following configuration:

   - __Field name__: ContactDynamics365RelatedID
      - __Data type__: Unique identifier (GUID)
      - __Display field in the editing form__: No
   - __Field name__: ContactDynamics365SynchronizedOn
      - __Data type__: Date and time
      - __Precision__: 7
      - __Display field in the editing form__: No

Finally, you must map the Xperience contact fields to the desired Dynamics 365 fields. Click the _"Edit"_ button next to the __Field mapping__ setting to open a new dialog window. This window displays the available Xperience contact field (including any custom fields added by your developers), and a drop-down list containing the Dynamics 365 fields. For each Xperience field, select the Dynamics 365 that it will be mapped to. If you leave a drop-down on _"(not mapped)"_, that Xperience field will not be synchronized to Dynamics 365.


### Enable incoming contact synchronization

Xperience contacts which are linked by the outgoing synchronization can optionally be updated whenever their information is changed in Dynamics 365. To use incoming synchronization, a custom .NET Web API endpoint is available in your administrative Xperience website at the URL _yourcms.com/xperience-dynamics365/updatecontact_. To change the Xperience contact information, you can use any approach you'd like to send a __POST__ request to this endpoint, where the body contains the Dynamics 365 contact information in JSON format.

> __Note__: Outgoing contact synchronization must be [enabled](#enable-outgoing-contact-synchronization) for incoming synchronization to work.

Typically, you will want to create this request using Dynamics 365's __Power Automate__ application, where you have several options:

- __Automated Cloud Flow__: The contact data will be sent to Xperience within seconds of changing in Dynamics 365.
- __Instant Cloud Flow__: The contact data is not automatically synchronized, but can be triggered manually by your Dynamics 365 users.
- __Scheduled Cloud Flow__: Synchronizes Dynamics 365 contacts to Xperience in batches based on a timer.

For this example, we will create an __Automated Cloud Flow__ to synchronize the contacts to Xperience instantly after they're changed in Dynamics 365.

1. Open the [Power Automate](https://powerautomate.microsoft.com/) application in Dynamics 365.
2. Click the __Create__ tab.
3. Click __Start from blank → Automated cloud flow__.
4. In the dialog box, create a flow name and select Microsoft Dataverse's "When a row is added, modified or deleted" trigger.
5. Configure the trigger to run when a contact is modified:

![Trigger definition](/Assets/flowtrigger.png)

6. Add a new step that uses the __HTTP__ action.
7. Set the action parameters to send a __POST__ request to the _/xperience-dynamics365/updatecontact_ endpoint of your Xperience administration website.
8. In the __Body__ parameter, use the "Dynamic content" menu to get the "Body" object, which is the contact information from the trigger.
9. Expand the "Show advanced options" menu and set the __Authentication__ to "Basic" and provide the username and password of an Xperience user with permissions to modify Xperience contacts. We recommend that you don't use an administrator account, but rather a specific user with limited [permissions](https://docs.xperience.io/managing-users/configuring-permissions).

![HTTP request definition](/Assets/httprequest.png)

Once this flow is saved and enabled, Dynamics 365 contact information will automatically be sent to your Xperience website when they are changed. If the Dynamics 365 contact's ID has been linked to an Xperience contact (via [outgoing synchronization](#enable-outgoing-contact-synchronization)), the Xperience contact will be updated by comparing the mapped fields and updating only those whose Xperience value are different from the Dynamics 365 value.

If you'd like to change how the incoming synchronization works, you can register your own implementation of the [`IDynamics365EntityMapper.MapChangedColumns`](/CMSModules/Kentico.Xperience.Dynamics365.Sales/Services/IDynamics365EntityMapper.cs#L49) method. This method runs when the Dynamics 365 contact information is sent to your Xperience website, and you are provided the Dynamics 365 contact, the Xperience contact that is linked, and the field mappings. The returned `Dictionary` should contain records where the records `Key` is the name of an Xperience contact field that should be updated, and the `Value` is the desired value of that field.

### Enable activity synchronization

Activity synchronization is performed by the scheduled task "Dynamics 365 activity synchronization" which is set to run every hour by default. However, you must first enable the synchronization by going to the __Settings__ application, navigating to the __Integration → Dynamics 365__ section, and checking the __Enabled__ box under the "Activity synchronization" category. Activity synchronization is not required for contact synchronization, but if you want to synchronize activities, you _must_ enable contact synchronization.

After enabling the synchronization, you can optionally choose a Dynamics 365 user or team in the __Default owner__ drop-down. All synchronized activities will be assigned to this user or team when they are created. If you leave this setting on _"(not set)"_ the activities will be unassigned.

## How the synchronization works

After [setting up the environment](#set-up-the-environment), you will find two new scheduled tasks in the __Scheduled tasks__ application of your administration. These tasks are enabled and set to run every hour by default, though you can disable them or adjust the interval as needed:

   - Dynamics 365 contact synchronization
   - Dynamics 365 activity synchronization

### Outgoing contact synchronization

 When the contact synchronization task runs, it will collect all Xperience contacts that have a score equal to or greater than the __Minimum score__ setting. The full contact data is sent to Dynamics 365 according to how you've [mapped the fields](#enable-contact-synchronization), and the ID of the Dynamics 365 contact that was created is stored in a custom Xperience field named "ContactDynamics365RelatedID." If activity synchronization is enabled, the contact's activities are also synchronized during creation of the contact. You can also synchronize a contact to Dynamics 365 regardless of their score by adding the __Import to Dynamics 365__ step to a [__Marketing automation__ process](https://docs.xperience.io/on-line-marketing-features/managing-your-on-line-marketing-features/marketing-automation/working-with-marketing-automation-processes).

Contacts that were _already_ synchronized in the past (e.g. the "ContactDynamics365RelatedID" fields contains a Dynamics 365 contact ID) are synchronized again during the task execution. The Xperience contact data is compared to the Dynamics 365 contact data to check whether the information was updated since the last synchronization. If no mapped fields have changed, synchronization is skipped for that contact. If some fields have changed, a partial update is made to the Dynamics 365 contact to avoid unwanted triggering of workflows (e.g. if you have a process that runs whenever a contact's email address is updated).

The Xperience contact fields that are available for mapping are automatically retrieved at run time, so any custom fields added by your developers will appear automatically. However, if you would like to adjust the retrieved fields for any reason, you can register a custom implementation of [`IContactFieldProvider`](/CMSModules/Kentico.Xperience.Dynamics365.Sales/Services/IContactFieldProvider.cs):

```cs
[assembly: RegisterImplementation(typeof(IContactFieldProvider), typeof(CustomContactFieldProvider), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.Default)]
namespace MyCompany.Customizations.Dynamics365
{
    public class CustomContactFieldProvider : IContactFieldProvider
    {
```

### Activity synchronization

This scheduled task loads all contacts that have been synchronized to Dynamics 365 and synchronizes their activities which were logged since the task's __Last run__ time. There is one exception- since activities are synchronized immediately when a new contact is created in Dynamics 365, this task will use the "ContactDynamics365SynchronizedOn" custom field to load only the activities logged after the synchronization time.

In order for an activity to be synchronized, the [activity type](https://docs.xperience.io/on-line-marketing-features/configuring-and-customizing-your-on-line-marketing-features/configuring-activities/adding-custom-activity-types) __Code name__ in Xperience must match the name of the entity in Dynamics 365. For example, Dynamics 365 contains the ["phonecall" entity](https://docs.microsoft.com/en-us/dynamics365/customer-engagement/web-api/phonecall?view=dynamics-ce-odata-9) out-of-the-box, which you can see by opening the __Power apps__ application and navigating to __Tables → Data__:

![Dynamics 365 entities](/Assets/entities.png)

To synchronize "phone call" activities from from Xperience to Dynamics 365, you would create a custom activity type whose __Code name__ matches the Dynamics 365 entity name:

![Phone call activity](/Assets/phonecall.png)

You can customize this behavior by developing your own implementation of [`IDynamicsEntityMapper`](/CMSModules/Kentico.Xperience.Dynamics365.Sales/Services/IDynamics365EntityMapper.cs):

```cs
[assembly: RegisterImplementation(typeof(IDynamics365EntityMapper), typeof(CustomEntityMapper), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.Default)]
namespace MyCompany.Customizations.Dynamics365.Sales
{
    /// <summary>
    /// A custom Entity mapper for Dynamics 365. 
    /// </summary>
    public class CustomEntityMapper : IDynamics365EntityMapper
    {
```

> __Note:__ When registering custom implementations of the integration's interfaces, make sure to include the original code found in [/CMSModules/Kentico.Xperience.Dynamics365.Sales/Services/Implementations/](/CMSModules/Kentico.Xperience.Dynamics365.Sales/Services/Implementations/).

The `MapActivityType` method is called while synchronizing activities to determine the name of the Dynamics 365 entity to create, using the passed `activityType` parameter. For example, if you want to create "phonecall" activities in Dynamics 365, but the code name of the activity in Xperience is "phone," your implementation would look something like this:

```cs
public string MapActivityType(string activityType)
{
   if (activityType.Equals("phone", StringComparison.OrdinalIgnoreCase))
   {
         return "phonecall";
   }

   return activityType;
}
```

Once the Xperience activity is configured to synchronize the Dynamics 365, the activity data must be mapped to the Dynamics 365 entity by your developers as there is no way for the integration to know where the information should be stored. Implement the `MapActivity` method which is called during activity synchronization to map Xperience activity data to an anonymous [`JObject`](https://www.newtonsoft.com/json/help/html/t_newtonsoft_json_linq_jobject.htm), which is sent to Dynamics 365. In this method, you will be provided with the name of the Entity being created (e.g. "mycustomactivity"), the ID of the Dynamics 365 contact that performed the activity, and the `relatedData` of the activity. The related data will be an `ActivityInfo` object when mapping standard Xperience activities.

## Example: synchronizing the "Page visit" activity

The Xperience "Page visit" activity is not synchronized by default, as there is no Dynamics 365 activity with the name "pagevisit." However, using the instructions in the above section, you can easily track what pages your contacts visited in Dynamics 365.

1. Open the [__Power Apps__](https://powerapps.microsoft.com/) application in Dynamics 365.
2. Expand the __Data__ tab and click __Tables__.
3. Click __New table__.
4. Set the desired table name and properties. Ensure that the __Table type__ is "Activity table:"

![Page visit creation](/Assets/pagevisittable.png)

5. Once the creation is finished, view the __Columns__ of the table and click __New column__.
6. Set the properties of the new column. In this case, we are creating a column to store the visited URL:

![Page URL creation](/Assets/pageurlcolumn.png)

7. Note the name of the new column (e.g. "cr0a3_pageurl") and the table name (e.g. "cr0a3_pagevisit").
8. In your Xperience CMS project, create a custom `IDynamics365EntityMapper` as described in the [Activity synchronization](#activity-synchronization) section. You can copy the code from [`DefaultDynamics365EntityMapper`](/CMSModules/Kentico.Xperience.Dynamics365.Sales/Services/Implementations/DefaultDynamics365EntityMapper.cs) to get started.

9. Use the `MapActivityType` method to translate the Xperience activity name "pagevisit" into your custom Dynamics 365 activity name noted in __step 7__:

```cs
public string MapActivityType(string activityType)
{
   if (activityType == PredefinedActivityType.PAGE_VISIT)
   {
         return "cr0a3_pagevisit";
   }

   return activityType;
}
```

10. Use the `MapActivity` method to add the desired data to the columns you added to the table in __step 7__:

```cs
public JObject MapActivity(string entityName, string dynamicsId, object relatedData)
{
   var entity = new JObject();
   MapCommonActivityProperties(dynamicsId, entity, relatedData);

   switch (entityName)
   {
         case "cr0a3_pagevisit":
            var activity = relatedData as ActivityInfo;
            entity.Add("cr0a3_pageurl", activity.ActivityURL);
            entity.Add("subject", $"Contact visited {activity.ActivityURL}");
            break;
         //...
   }

   DecodeValues(entity);

   return entity;
}
```

11. Build the project

With this customization, when your Xperience contacts visit pages on your website, the Xperience "Page visit" activity will be automatically synchronized by the "Dynamics 365 activity synchronization" scheduled task and can be viewed directly in the Dynamics 365 contact's timeline.

## Creating Tasks and Appointments

This integration contains two custom Marketing automation actions which will create Dynamics 365 activities when they execute:

- Create Dynamics 365 task
- Create Dynamics 365 appointment

With the __Create Dynamics 365 task__ action, you can create a task for your Dynamics 365 users to complete. For example, if you want to send flowers to everyone who submits a form on your site, the process could look like this:

![Task automation process](/Assets/taskprocess.png)

The task will be assigned to the user or team specified by the __Default owner__ setting, or unassigned if not set. You can also create an appointment in Dynamics 365 using the __Create Dynamics 365 appointment__ action. For example, if a partner submits a form indicating they'd like to have a meeting to discuss a new opportunity, the process might look like this:

![Appointment automation process](/Assets/appointmentprocess.png)

The appointment can contain required and optional attendees that you choose from your Dynamics 365 users. The appointment will always include the contact that is currently in the automation process.

## Automating Microsoft Teams messages

Using the magic of __Power Automate__, your team can be automatically notified of new Dynamics 365 contacts or activities when they are created. For example, if a visitor on your site fills out a "Contact Us" form requesting more information about your products, a new post can be created in your Teams "Sales" channel for your Sales team to follow up immediately.

![Teams sample message](/Assets/teamsmessage.png)

To accomplish this, first you need to synchronize the Kentico Xperience "Form submission" activity to Dynamics 365. You can follow [this example](#example-synchronizing-the-page-visit-activity) to create a new activity type in Dynamics 365 for form submissions. When creating custom columns in the table, add one called "Form name" to hold the code name of the Xperience form. The mapping logic in your `MapActivity` implementation could look something like this:

```cs
case "cr0a3_formsubmission":
   var activity = relatedData as ActivityInfo;
   var formInfo = BizFormInfo.Provider.Get(activity.ActivityItemID);
   var dataClass = DataClassInfoProvider.GetDataClassInfo(formInfo.FormClassID);
   var formDetail = BizFormItemProvider.GetItem(activity.ActivityItemDetailID, dataClass.ClassName);
   var activityBody = new StringBuilder();
   foreach (var column in formDetail.ColumnNames)
   {
      var submittedData = formDetail.GetStringValue(column, String.Empty);
      if (!String.IsNullOrEmpty(submittedData))
      {
            activityBody.Append($"{column}: {submittedData}\r\n");
      }
   }

   entity.Add("subject", $"Contact submitted form '{formInfo.FormDisplayName}'");
   entity.Add("cr0a3_formname", formInfo.FormName);
   entity.Add("description", activityBody.ToString());
   break;
```

With your custom code in place, you should start to see form submission activities automatically synchronized from Xperience to Dynamics 365. Now, it's time to send a Teams message when that happens!

1. Open the [Power Automate](https://powerautomate.microsoft.com/) application in Dynamics 365.
2. Click the __Create__ tab.
3. Click __Start from blank → Automated cloud flow__.
4. In the dialog box, create a flow name and select Microsoft Dataverse's "When a row is added, modified or deleted" trigger.
5. Configure the trigger to run when a form submission is added.
6. Add a __Condition__ control after the trigger. This control will check the custom "Form name" column added to the activity, which contains the Xperience form name. Since we want to only send Teams messages for the "Contact Us" form, copy the code name from Xperience's __Forms application → (edit form) → General tab → Form code name__ field.

After adding the __Condition__ control, you will see two paths you can add more actions to:

![Teams flow condition control](/Assets/teamsflowconditon.png)

7. In the "If yes" path, add the __Get a row by ID__ action to find the contact that submitted the form:

![Teams flow get contact](/Assets/teamsflowgetcontact.png)

8. After that, add a Microsoft Teams __Post message in chat or channel__ action. Here, you can construct the body of the message using dynamic data from the activity (trigger) and the contact (from the previous step). In the sample code posted above, we set the form data to the Dynamics 365 activity's __description__ field, so we can include that in the body.  
If you want to provide a direct link to the Dynamics 365 contact, you can use the approach described in [this article](https://d365demystified.com/2020/06/13/generate-dynamics-365-record-link-in-a-flow-using-cds-connector-power-automate/). In this example, we used the function `uriHost(body('Get_a_row_by_ID')?['@odata.id'])` to get the URL of the Dynamics 365 tenant. To replace the line breaks in the activity description for better Teams formatting, you can use `uriComponentToString(replace(uriComponent(triggerBody()['description']), '%0A', '<br/>'))`  
The finished message looks something like this:

![Teams flow message body](/Assets/teamsflowmessagebody.png)

9. Finally, add a __Terminate__ control to the "If no" path to end the flow if the form name doesn't match the "Contact Us" form.

Once you save and enable the flow, you've successfully automated the synchronizing of Xperience form submissions and their data to Dynamics 365, and Teams messages containing the form data and a link to the contact!

## Contributing

If you'd like to contribute to this project, please see [CONTRIBUTING.md](/CONTRIBUTING.md) for more details on how to start. When you've made your code changes and are ready to submit a pull request, you will need to [export](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/exporting-objects) the Dynamics 365 custom module and code files from the Xperience __Sites__ application:

1. Open the __Modules__ application and edit the __Dynamics 365__ module.
2. On the __General tab__, increment the __Module version__ using [SemVer](https://semver.org/) standards. E.g., increment the patch version for backwards compatible bug fixes, or the minor version for new backwards compatible functionality.
3. Clear all settings related to the integration. This can be done in __Settings → Integration → Dynamics 365__, or in the database using a query like below. Use caution when querying the database directly and ensure that you have a backup and you modify the query as needed!

```sql
UPDATE CMS_SettingsKey SET KeyValue = NULL WHERE KeyName LIKE 'dynamics365%'
```

4. Open the __Sites__ application and click the __Export__ button.
5. Name the export file using the format `Kentico.Xperience.Dynamics365.Sales.X.Y.Z.zip`, where _"X.Y.Z"_ is the version set in step #2.
6. Select the _"(no site, only global objects)"_ and _"Do not preselect any objects"_ options and click  __Next__.
7. Uncheck __All objects → Export tasks__.
8. In __Global objects → On-line marketing → Automation actions__, check the following options (and any you've added):

  - Create Dynamics 365 appointment
  - Create Dynamics 365 task
  - Import to Dynamics 365

9. In __Global objects → Development → Form controls__, check the following options, check the following options (and any you've added):

  - Dynamics 365 option set selector
  - Dynamics 365 user selector

10. In __Global objects → Development → Modules__, check the _"Dynamics 365"_ option.
11. In __Global objects → Configuration → Scheduled tasks__, check the following options, check the following options (and any you've added):

  - Dynamics 365 activity synchronization
  - Dynamics 365 contact synchronization

12. In __Global objects → Configuration → Settings keys__, check the following options, check the following options (and any you've added). They should be located on the final page of the list:

  - Default owner (Dynamics365DefaultOwner)
  - Enabled (Dynamics365ContactSyncEnabled)
  - Enabled (Dynamics365ActivitySyncEnabled)
  - Field mapping (Dynamics365ContactMapping)
  - Minimum score (Dynamics365MinScore)

  13. Include any other objects from other categories that you've added. You can see [our documentation](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/exporting-objects) for more information about exporting objects.
  14. When you're finished selecting export objects, click __Next__ and the export package will be created in the appropriate location and included in the repository.