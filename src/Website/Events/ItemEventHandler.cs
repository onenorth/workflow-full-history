using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Data.Items;
using Sitecore.Events;
using Sitecore.Workflows.Simple;

namespace OneNorth.WorkflowFullHistory.Events
{
    public class ItemEventHandler
    {
        protected void OnItemSaving(object sender, EventArgs args)
        {
            try
            {
                var newItem = Event.ExtractParameter(args, 0) as Item;

                if (newItem == null || newItem.Database.DataManager.GetWorkflowInfo(newItem) == null)
                    return;

                var originalItem = newItem.Database.GetItem(newItem.ID, newItem.Language, newItem.Version);

                var differences = FindDifferences(newItem, originalItem);

                if (differences.Any())
                {
                    var message = string.Format("Item content changed [{0}]", string.Join(", ", differences));

                    AddHistoryEntry(newItem, message);
                }
            }
            catch (Exception)
            {
                //history write error
            }
        }

        private static List<string> FindDifferences(Item newItem, Item originalItem)
        {
            newItem.Fields.ReadAll();

            var fieldNames = newItem.Fields.Where(newItemField => !newItemField.Name.StartsWith("__") && newItem[newItemField.Name] != originalItem[newItemField.Name])
                .Select(field => field.DisplayName)
                .ToList();

            return fieldNames;
        }

        private static void AddHistoryEntry(Item item, string text)
        {
            var workflowProvider = (item.Database.WorkflowProvider as WorkflowProvider);
            if (workflowProvider != null && workflowProvider.HistoryStore != null)
            {
                var workflowState = GetWorkflowState(item);
                workflowProvider.HistoryStore.AddHistory(item, workflowState, workflowState, text);
            }
        }

        private static string GetWorkflowState(Item item)
        {
            var info = item.Database.DataManager.GetWorkflowInfo(item);
            return (info != null) ? info.StateID : string.Empty;
        }
    }
}