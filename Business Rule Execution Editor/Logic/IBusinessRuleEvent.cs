using Microsoft.Xrm.Sdk;

namespace Business_Rule_Execution_Editor.Logic
{
    internal interface IBusinessRuleEvent
    {
        int Rank { get; set; }
        string TriggerEvent { get; set; }
        string EntityLogicalName { get; }

        OptionSetValue TriggerScope { get; }

        string ControlName { get; }

        string Name { get; }

        string Description { get; }

        bool HasChanged { get; }

        string Type { get; }

        void UpdateRank(IOrganizationService service);
    }
}