using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using Sitecore.Data;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI;
using Sitecore.Web.UI.Sheer;
using Sitecore.Workflows.Simple;

namespace OneNorth.WorkflowFullHistory.Shell.Framework.Commands
{
    public class WorkflowFullHistory : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            if (context.Items.Length != 1)
                return CommandState.Hidden;

            var item = context.Items[0];

            var workflowProvider = item.Database.WorkflowProvider;
            if (item == null || workflowProvider == null || workflowProvider.GetWorkflow(item) == null)
                return CommandState.Hidden;

            return CommandState.Enabled;
        }

        public override void Execute(CommandContext context)
        {
            var item = context.Items[0];

            var parameters = new System.Collections.Specialized.NameValueCollection();
            parameters["database"] = item.Database.Name;
            parameters["itemid"] = item.ID.ToString();
            parameters["language"] = item.Language.Name;
            Sitecore.Context.ClientPage.Start(this, "Run", parameters);
        }

        protected void Run(ClientPipelineArgs args)
        {
            if (args.IsPostBack)
                return;

            var database = Database.GetDatabase(args.Parameters["database"]);
            var itemId = ID.Parse(args.Parameters["itemid"]);
            var language = Sitecore.Globalization.Language.Parse(args.Parameters["language"]);

            var workflowProvider = (database.WorkflowProvider as WorkflowProvider);
            if (workflowProvider == null || workflowProvider.HistoryStore == null)
                return;

            var item = database.GetItem(itemId, language);

            var assembly = Assembly.GetExecutingAssembly();
            string itemHtml;
            using (var stream = assembly.GetManifestResourceStream("OneNorth.WorkflowFullHistory.Shell.Framework.Commands.WorkflowFullHistoryItem.html"))
            using (var reader = new StreamReader(stream))
                itemHtml = reader.ReadToEnd();

            var html = new StringBuilder();
            foreach (var version in item.Versions.GetVersions())
            {
                var workflowEvents = workflowProvider.HistoryStore.GetHistory(version);

                foreach (var workFlowEvent in workflowEvents)
                {
                    var oldStateItem = (ID.IsID(workFlowEvent.OldState))
                                           ? database.GetItem(ID.Parse(workFlowEvent.OldState))
                                           : null;
                    var oldState = (oldStateItem != null) ? oldStateItem.DisplayName : "?";

                    var newStateItem = (ID.IsID(workFlowEvent.NewState))
                                           ? database.GetItem(ID.Parse(workFlowEvent.NewState))
                                           : null;
                    var newState = (newStateItem != null) ? newStateItem.DisplayName : "?";
                    var iconUrl = (newStateItem != null) ? Sitecore.Resources.Images.GetThemedImageSource(newStateItem.Appearance.Icon, ImageDimension.id24x24) : "";

                    html.AppendFormat(itemHtml, 
                        iconUrl,
                        workFlowEvent.User, 
                        workFlowEvent.Date.ToString("D"),
                        version.Version.Number, 
                        oldState, 
                        newState, 
                        HttpUtility.HtmlEncode(workFlowEvent.Text));
                }
            }

            string popupHtml;
            using (var stream = assembly.GetManifestResourceStream("OneNorth.WorkflowFullHistory.Shell.Framework.Commands.WorkflowFullHistoryPopup.html"))
            using (var reader = new StreamReader(stream))
                popupHtml = reader.ReadToEnd();

            Sitecore.Context.ClientPage.ClientResponse.ShowPopup(Guid.NewGuid().ToString(), "below", string.Format(popupHtml, html));
        }
    }
}