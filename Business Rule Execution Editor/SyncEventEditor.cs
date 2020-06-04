using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Business_Rule_Execution_Editor.Logic;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace Business_Rule_Execution_Editor
{
    public partial class SyncEventEditor : PluginControlBase, IPayPalPlugin
    {
        // Public Properties
        public string DonationDescription => "Synchronous Event Execution Order Editor Fan Club!";
        public string EmailAccount => "jlax58@gmail.com";

        // Private Properties
        private List<IBusinessRuleEvent> _events;
        private Settings _mySettings;

        public SyncEventEditor()
        {
            InitializeComponent();
        }

        private void MyPluginControl_Load(object sender, EventArgs e)
        {
            // ShowInfoNotification("This is a notification that can lead to XrmToolBox repository",new Uri("https://github.com/MscrmTools/XrmToolBox"));

            // Loads or creates the settings for the plugin
            if (!SettingsManager.Instance.TryLoad(GetType(), out _mySettings))
            {
                _mySettings = new Settings();

                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings found and loaded");
            }
        }

        #region Load Events

        private void tsbLoadEvents_Click(object sender, EventArgs e)
        {
            ExecuteMethod(LoadEvents);
        }

        private void LoadEvents()
        {
            tvEvents.Nodes.Clear();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading Sdk message filters...",
                Work = (bw, e) =>
                {
                    _events = new List<IBusinessRuleEvent>();

                    bw.ReportProgress(0, "Loading Entities...");

                    _events.AddRange(BusinessRule.RetrieveAllBusinessRuleSteps(Service));
                },
                PostWorkCallBack = e =>
                {
                    if (e.Error != null)
                    {
                        MessageBox.Show(ParentForm, "An error occured: " + e.Error, "Error", MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    else
                    {
                        TreeViewHelper tvh = new TreeViewHelper(tvEvents);

                        foreach (IBusinessRuleEvent sEvent in _events) tvh.AddSynchronousEvent(sEvent);
                    }
                },
                ProgressChanged = e =>
                {
                    // it will display "I have found the user id" in this example
                    SetWorkingMessage(e.UserState.ToString());
                }
            });
        }

        #endregion

        #region Create Event Rows After Select

        private void tvEvents_AfterSelect(object sender, TreeViewEventArgs e)
        {
            dgvSynchronousEvent.Rows.Clear();

            if (e.Node.Nodes.Count > 0) return;

            List<IBusinessRuleEvent> localEvents = (List<IBusinessRuleEvent>)e.Node.Tag;

            foreach (IBusinessRuleEvent sEvent in localEvents)
            {
                DataGridViewRow row = new DataGridViewRow {Tag = sEvent};
                row.Cells.Add(new DataGridViewTextBoxCell { Value = sEvent.Rank });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = sEvent.TriggerEvent });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = sEvent.Type });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = sEvent.Name });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = sEvent.Description });
                dgvSynchronousEvent.Rows.Add(row);
            }
        }

        #endregion

        #region Rank Update

        private void dgvSynchronousEvent_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvSynchronousEvent.Rows.Count == 0) return;
            dgvSynchronousEventRank.ValueType = typeof(int);

            if (int.TryParse(dgvSynchronousEvent.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString(), out var rank))
            {
                var sEvent = (IBusinessRuleEvent)dgvSynchronousEvent.Rows[e.RowIndex].Tag;
                sEvent.Rank = rank;

                dgvSynchronousEvent.Sort(dgvSynchronousEvent.Columns[e.ColumnIndex], ListSortDirection.Ascending);
            }
            else
            {
                MessageBox.Show(ParentForm, "Only integer value is allowed for rank", "Warning", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void tsbUpdate_Click(object sender, EventArgs e)
        {
            var updatedEvents = _events.Where(ev => ev.HasChanged);

            //if (updatedEvents.Any(ev => ev.Type == "Workflow") && DialogResult.No ==
               //MessageBox.Show(ParentForm,
                 //   Resources.SyncEventEditor_tsbUpdate_Click_Workflows_will_be_deactivated__updated__then_activated_back__Are_you_sure_you_want_to_continue_,
                   // Resources.SyncEventEditor_tsbUpdate_Click_Question, MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                //return;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Updating...",
                Work = (bw, evt) =>
                {
                    foreach (var sEvent in _events.Where(ev => ev.HasChanged))
                    {
                        bw.ReportProgress(0, $"Updating {sEvent.Type} {sEvent.Name}");
                        sEvent.UpdateRank(Service);
                    }
                },
                PostWorkCallBack = evt =>
                {
                    if (evt.Error != null)
                        MessageBox.Show(ParentForm, $"An error occured: {evt.Error.Message}", "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                },
                ProgressChanged = evt =>
                {
                    // it will display "I have found the user id" in this example
                    SetWorkingMessage(evt.UserState.ToString());
                }
            });
        }

        #endregion

        #region On Close of Plugin

        private void tsbClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }


        /// <summary>
        ///     This event occurs when the plugin is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SyncEventExecEditor_OnClose(object sender, EventArgs e)
        {
            // Before leaving, save the settings
            SettingsManager.Instance.Save(GetType(), _mySettings);
        }

        #endregion

        #region Update Connection

        /// <summary>
        ///     This event occurs when the connection has been updated in XrmToolBox
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail,
            string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            if (_mySettings == null || detail == null) return;
            _mySettings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
            LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);
        }

        #endregion

        #region Keyboard Shortcuts

        public void ReceiveKeyDownShortcut(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5 && tsbLoadEvents.Enabled)
            {
                tsbLoadEvents_Click(null, null);
            }
            else if (e.Control && e.KeyCode == Keys.S && tsbUpdate.Enabled)
            {
                tsbUpdate_Click(null, null);
            }
        }

        #endregion

        // Event to open record?
        private void dgvSynchronousEvent_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }
    }
}