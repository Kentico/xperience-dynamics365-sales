using CMS.Core;
using CMS.FormEngine.Web.UI;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.UI.WebControls;

namespace Kentico.Xperience.Dynamics365.Sales.Controls
{
    /// <summary>
    /// A form control which allows the selection of a Dynamics 365 systemuser or team (if enabled).
    /// </summary>
    public partial class UserTeamSelector : FormEngineUserControl
    {
        private string mValue;
        private IDynamics365Client dynamics365Client;


        /// <summary>
        /// If true, teams can be selected.
        /// </summary>
        private bool AllowTeams
        {
            get
            {
                return GetValue("AllowTeams", true);
            }
        }


        public override object Value
        {
            get
            {
                return drpOwner.SelectedValue;
            }
            set
            {
                mValue = ValidationHelper.GetString(value, String.Empty);
            }
        }


        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            dynamics365Client = Service.Resolve<IDynamics365Client>();

            drpOwner.Enabled = true;
            drpOwner.Items.Clear();
            drpOwner.Items.Add(new ListItem("(not set)", String.Empty));

            try
            {
                var userListItems = GetSystemUsers();
                drpOwner.Items.AddRange(userListItems.ToArray());

                if (AllowTeams)
                {
                    var teamListItems = GetTeams();
                    drpOwner.Items.AddRange(teamListItems.ToArray());
                }

                if (drpOwner.Items.Count == 1)
                {
                    drpOwner.Enabled = false;
                    drpOwner.ToolTip = "Unable to load selections, please check the Event Log.";
                };
                
                if (!String.IsNullOrEmpty(mValue) && drpOwner.Items.FindByValue(mValue) != null)
                {
                    drpOwner.SelectedValue = mValue;
                }
            }
            catch (InvalidOperationException ex)
            {
                drpOwner.Enabled = false;
                drpOwner.ToolTip = "Unable to load selections, please check the Event Log.";
            }
        }


        private IEnumerable<ListItem> GetSystemUsers()
        {
            var userArray = dynamics365Client.GetSystemUsers();

            return userArray.Where(user => user.AccessMode < ((int)AccessModeEnum.NonInteractive)).Select(user =>
            {
                var text = $"{user.FullName} (user)";
                var value = $"/{Dynamics365Constants.ENTITY_USER}s({user.SystemUserId})";
                return new ListItem(text, value);
            });
        }


        private IEnumerable<ListItem> GetTeams()
        {
            return Service.Resolve<IProgressiveCache>().Load(cacheSettings => {
                cacheSettings.CacheDependency = new CMSCacheDependency()
                {
                    CacheKeys = new string[]
                    {
                        $"cms.settingskey|{Dynamics365Constants.SETTING_CLIENTID.ToLower()}",
                        $"cms.settingskey|{Dynamics365Constants.SETTING_SECRET.ToLower()}",
                        $"cms.settingskey|{Dynamics365Constants.SETTING_TENANTID.ToLower()}",
                        $"cms.settingskey|{Dynamics365Constants.SETTING_SECRET.ToLower()}"
                    }
                };

                try
                {
                    var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_GET_POST, Dynamics365Constants.ENTITY_TEAM) + "?$select=teamid,name";
                    var response = dynamics365Client.SendRequest(endpoint, HttpMethod.Get);
                    if (!response.IsSuccessStatusCode)
                    {
                        cacheSettings.Cached = false;
                        return Enumerable.Empty<ListItem>();
                    }

                    var sourceJson = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var jObject = JObject.Parse(sourceJson);
                    var teamArray = jObject.Value<JArray>("value");

                    return teamArray.Select(team =>
                    {
                        var text = $"{team.Value<string>("name")} (team)";
                        var value = $"/{Dynamics365Constants.ENTITY_TEAM}s({team.Value<string>("teamid")})";
                        return new ListItem(text, value);
                    });
                }
                catch (Exception ex)
                {
                    cacheSettings.Cached = false;
                    Service.Resolve<IEventLogService>().LogError(nameof(UserTeamSelector), nameof(GetTeams), ex.Message);

                    return Enumerable.Empty<ListItem>();
                }
                
            }, new CacheSettings(TimeSpan.FromMinutes(Dynamics365Constants.CACHE_MINUTES).TotalMinutes, $"{nameof(UserTeamSelector)}|{nameof(GetTeams)}"));
        }
    }
}