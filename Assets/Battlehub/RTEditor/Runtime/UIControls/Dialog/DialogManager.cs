using UnityEngine;
using Battlehub.UIControls.DockPanels;
using System.Collections.Generic;

namespace Battlehub.UIControls.Dialogs
{
    public class DialogManager : MonoBehaviour
    {
        [SerializeField]
        private DockPanel m_dockPanels = null;

        [SerializeField]
        private Dialog m_dialogPrefab = null;

        private Stack<Dialog> m_dialogStack = new Stack<Dialog>();

        public event DialogAction DialogDestroyed;

        public bool IsDialogOpened
        {
            get { return m_dialogStack.Count > 0; }
        }

        public void CloseDialog()
        {
            if(IsDialogOpened)
            {
                m_dialogStack.Peek().Close(false);
            }
        }

        public Dialog ShowDialog(Sprite icon, string header, Transform content,
             DialogAction<DialogCancelArgs> okAction = null, string okText = "OK",
             DialogAction<DialogCancelArgs> cancelAction = null, string cancelText = "Cancel",
             float minWidth = 420,
             float minHeight = 200,
             float preferredWidth = 700, 
             float preferredHeight = 400, 
             bool canResize = true)
        {
            Dialog dialog = ShowDialog(icon, header,  okAction, okText, cancelAction, cancelText, minWidth, minHeight, preferredWidth, preferredHeight, canResize);
            dialog.Content = content;
            m_dockPanels.AddModalRegion(dialog.HeaderRoot, dialog.transform, minWidth, minHeight, new Rect(0, 0, preferredWidth, preferredHeight), true, canResize);
            m_dialogStack.Push(dialog);
            return dialog;
        }

        public Dialog ShowDialog(Sprite icon, string header, string content,
            DialogAction<DialogCancelArgs> okAction = null, string okText = "OK",
            DialogAction<DialogCancelArgs> cancelAction = null, string cancelText = "Cancel",
            float minWidth = 350,
            float minHeight = 115,
            float preferredWidth = 350,
            float preferredHeight = 100,
            bool canResize = false)
        {
            Dialog dialog = ShowDialog(icon, header, okAction, okText, cancelAction, cancelText, minWidth, minHeight, preferredWidth, preferredHeight, canResize);
            dialog.ContentText = content;
            m_dockPanels.AddModalRegion(dialog.HeaderRoot, dialog.transform, minWidth, minHeight, new Rect(0, 0, preferredWidth, preferredHeight), true, canResize);
            m_dialogStack.Push(dialog);
            return dialog;
        }

        private Dialog ShowDialog(Sprite icon, string header,
            DialogAction<DialogCancelArgs> okAction = null, string okText = "OK",
            DialogAction<DialogCancelArgs> cancelAction = null, string cancelText = "Cancel",
            float minWidth = 350, 
            float minHeight = 115,
            float preferredWidth = 350,
            float preferredHeight = 100,
            bool canResize = false)
        {
            if(m_dialogStack.Count > 0)
            {
                Dialog previousDialog = m_dialogStack.Peek();
                previousDialog.Hide();
            }

            Dialog dialog = Instantiate(m_dialogPrefab);
            dialog.name = "Dialog " + header;
            dialog.Icon = icon;
            dialog.HeaderText = header;
            
            dialog.OkAction = okAction;
            dialog.OkText = okText;
            if(cancelAction != null)
            {
                dialog.CancelAction = cancelAction;
                dialog.CancelText = cancelText;
                dialog.IsCancelVisible = true;
            }
            else
            {
                dialog.IsCancelVisible = false;
            }
            
            dialog.Closed += OnDestroyed;
            return dialog;      
        }

        private void OnDestroyed(Dialog sender, bool? result)
        {
            sender.Closed -= OnDestroyed;
            if (m_dialogStack.Contains(sender))
            {
                while(m_dialogStack.Count > 0)
                {
                    Dialog dialog = m_dialogStack.Pop();
                    
                    if (sender == dialog)
                    {
                        if (DialogDestroyed != null)
                        {
                            DialogDestroyed(dialog);
                        }
                        dialog.Close();
                        if (m_dialogStack.Count > 0)
                        {
                            Dialog previousDialog = m_dialogStack.Peek();
                            previousDialog.Show();
                        }
                        break;
                    }

                    dialog.Close();
                }
            }
        }
    }
}

