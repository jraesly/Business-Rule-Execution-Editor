using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Business_Rule_Execution_Editor.Logic
{
    internal class TreeViewHelper
    {
        private readonly TreeView _tv;

        public TreeViewHelper(TreeView tv)
        {
            _tv = tv;
            tv.Sorted = true;
        }

        public void AddSynchronousEvent(IBusinessRuleEvent sEvent)
        {
            var entityNode = _tv.Nodes.Find(sEvent.EntityLogicalName, false).ToList().SingleOrDefault();
            if (entityNode == null)
            {
                entityNode = new TreeNode(sEvent.EntityLogicalName)
                    {ImageIndex = 0, SelectedImageIndex = 0, Name = sEvent.EntityLogicalName};
                _tv.Nodes.Add(entityNode);
            }

            var attributeNode = entityNode.Nodes.Find(sEvent.ControlName, false).ToList().SingleOrDefault();
            if (attributeNode == null)
            {
                attributeNode = new TreeNode(sEvent.ControlName)
                {
                    ImageIndex = 1, SelectedImageIndex = 1, Name = sEvent.ControlName,
                    Tag = new List<IBusinessRuleEvent>()
                };
                entityNode.Nodes.Add(attributeNode);
            }

            ((List<IBusinessRuleEvent>) attributeNode.Tag).Add(sEvent);
        }
    }
}