using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Business_Rule_Execution_Editor.Logic
{
    internal partial class BusinessRule : IBusinessRuleEvent
    {
        public BusinessRule(Entity businessrule)
        {
            this._businessrule = businessrule;
            _initialRank = Rank;

        }


        public void UpdateRank(IOrganizationService service)
        {
            // Return if value has not changed
            if (!HasChanged) return;
            
            // Retrieve workflow 
            Entity wf = service.Retrieve("workflow", _businessrule.Id, new ColumnSet("statecode"));
            
            // If Workflow is active then set state to Draft
            if (wf.GetAttributeValue<OptionSetValue>("statecode").Value != 0)
                service.Execute(new SetStateRequest
                {
                    EntityMoniker = wf.ToEntityReference(),
                    State = new OptionSetValue(0),
                    Status = new OptionSetValue(-1)
                });

            _businessrule.Attributes.Remove("statecode");
            _businessrule.Attributes.Remove("statuscode");
            service.Update(_businessrule);

            if (wf.GetAttributeValue<OptionSetValue>("statecode").Value != 0)
                service.Execute(new SetStateRequest
                {
                    EntityMoniker = wf.ToEntityReference(),
                    State = new OptionSetValue(1),
                    Status = new OptionSetValue(-1)
                });
            _initialRank = _businessrule.GetAttributeValue<int>("rank");
        }

        public static IEnumerable<BusinessRule> RetrieveAllBusinessRuleSteps(IOrganizationService service)
        {
            // Instantiate QueryExpression qe
            QueryExpression qe = new QueryExpression("workflow");

            // Add columns to QEworkflow.ColumnSet
            qe.ColumnSet.AddColumns(Constants.ModifiedOn, Constants.Name, Constants.PrimaryEntity, Constants.ProcessTriggerScope, Constants.statecode, Constants.statuscode);
            qe.AddOrder(Constants.ModifiedOn, OrderType.Descending);

            // Define filter QEworkflow.Criteria
            qe.Criteria.AddCondition(Constants.category, ConditionOperator.Equal, 2); // Business Rule
            //qe.Criteria.AddCondition("primaryentity", ConditionOperator.Equal, QEworkflow_primaryentity);
            qe.Criteria.AddCondition(Constants.statecode, ConditionOperator.Equal, 1); // Activated

            // Add link-entity qe_processtrigger
            LinkEntity qeProcesstrigger = qe.AddLink("processtrigger", "workflowid", "processid", JoinOperator.Inner);
            qeProcesstrigger.EntityAlias = "trigger";

            // Add columns to QEworkflow_processtrigger.Columns
            qeProcesstrigger.Columns.AddColumns(Constants.scope, Constants.Event, Constants.primaryentitytypecode, Constants.controlname, Constants.processid, Constants.controltype);

            EntityCollection steps = service.RetrieveMultiple(qe);

            return steps.Entities.Select(e => new BusinessRule(e));
        }
    }

    internal partial class BusinessRule
    {
        private readonly Entity _businessrule;
        private int _initialRank;

        public int Rank { get; set; }

        public string TriggerEvent
        {
            get => _businessrule.GetAttributeValue<string>("trigger.event");
            set => _businessrule[""] = value;
        }

        public string EntityLogicalName => _businessrule.GetAttributeValue<string>("primaryentity");
        public OptionSetValue TriggerScope => _businessrule.GetAttributeValue<OptionSetValue>("trigger.scope");
        public string ControlName => _businessrule.GetAttributeValue<string>("trigger.controlname");
        public int Stage { get; }
        public string Message { get; }
        public string Name => _businessrule.GetAttributeValue<string>("name");
        public string Description => _businessrule.GetAttributeValue<string>("description");
        public bool HasChanged => _initialRank != Stage;
        public string Type => "Business Rule";
    }
}