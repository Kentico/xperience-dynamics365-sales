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
    public partial class UserTeamSelector : FormEngineUserControl
    {
        private string mValue;
        private IDynamics365Client dynamics365Client;


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


        protected void Page_Load(object sender, EventArgs e)
        {
            dynamics365Client = Service.Resolve<IDynamics365Client>();

            var userListItems = GetSystemUsers();
            drpOwner.Items.Add(new ListItem("(not set)", String.Empty));
            drpOwner.Items.AddRange(userListItems.ToArray());

            if (AllowTeams)
            {
                var teamListItems = GetTeams();
                drpOwner.Items.AddRange(teamListItems.ToArray());
            }

            if (!String.IsNullOrEmpty(mValue))
            {
                drpOwner.SelectedValue = mValue;
            }
        }


        private IEnumerable<ListItem> GetSystemUsers()
        {
            var userArray = dynamics365Client.GetSystemUsers().ConfigureAwait(false).GetAwaiter().GetResult();

            return userArray.Where(user => user.AccessMode < ((int)AccessModeEnum.NonInteractive)).Select(user =>
            {
                var text = $"{user.FullName} (user)";
                var value = $"/systemusers({user.SystemUserId})";
                return new ListItem(text, value);
            });
        }


        private IEnumerable<ListItem> GetTeams()
        {
            var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_GET_POST, "team") + "?$select=teamid,name";
            var response = dynamics365Client.SendRequest(endpoint, HttpMethod.Get).ConfigureAwait(false).GetAwaiter().GetResult();
            var sourceJson = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var jObject = JObject.Parse(sourceJson);
            var teamArray = jObject.Value<JArray>("value");

            return teamArray.Select(user =>
            {
                var text = $"{user.Value<string>("name")} (team)";
                var value = $"/teams({user.Value<string>("teamid")})";
                return new ListItem(text, value);
            });
        }
    }
}