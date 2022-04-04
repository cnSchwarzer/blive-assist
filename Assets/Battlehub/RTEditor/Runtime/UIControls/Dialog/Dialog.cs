using Battlehub.UIControls.DockPanels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.UIControls.Dialogs
{
    public class DialogCancelArgs
    {
        public bool Cancel;
    }

    public delegate void DialogAction(Dialog sender);
    public delegate void DialogAction<T>(Dialog sender, T args);

    public class Dialog : MonoBehaviour
    {
        private Region m_parentRegion;
        public Region ParentRegion
        {
            get { return m_parentRegion; }
        }

        public event DialogAction<DialogCancelArgs> Ok;
        public event DialogAction<DialogCancelArgs> Cancel;
        public event DialogAction<bool?> Closed;
        public DialogAction<DialogCancelArgs> OkAction;
        public DialogAction<DialogCancelArgs> CancelAction;

        [SerializeField]
        private Transform m_headerRoot = null;
        public Transform HeaderRoot
        {
            get { return m_headerRoot; }
        }

        [SerializeField]
        private Transform m_contentParent = null;

        [SerializeField]
        private Image m_headerIcon = null;

        [SerializeField]
        private TextMeshProUGUI m_headerText = null;

        [SerializeField]
        private TextMeshProUGUI m_contentText = null;

        [SerializeField]
        private Transform m_buttonsRoot = null;

        [SerializeField]
        private Button m_okButton = null;

        [SerializeField]
        private Button m_cancelButton = null;

        [SerializeField]
        private Button m_closeButton = null;

        public Sprite Icon
        {
            set
            {
                if(m_headerIcon != null)
                {
                    m_headerIcon.sprite = value;
                    if(value != null)
                    {
                        m_headerIcon.gameObject.SetActive(true);
                    }
                }
            }
        }

        public string HeaderText
        {
            set
            {
                if(m_headerText != null)
                {
                    m_headerText.text = value;
                }
            }
        }

        public string ContentText
        {
            set
            {
                m_contentText.text = value;
            }
        }

        public Transform Content
        {
            get
            {
                if(m_contentParent == null)
                {
                    return null;
                }
                if(m_contentParent.childCount == 0)
                {
                    return null;
                }

                return m_contentParent.GetChild(0);
            }
            set
            {
                if(m_contentParent != null)
                {
                    foreach(Transform child in m_contentParent)
                    {
                        Destroy(child.gameObject);
                    }
                    
                    value.SetParent(m_contentParent, false);

                    RectTransform rt = (RectTransform)value;
                    rt.Stretch();
                }
            }
        }

        public string OkText
        {
            set
            {
                if(m_okButton != null)
                {
                    TextMeshProUGUI okButtonText = m_okButton.GetComponentInChildren<TextMeshProUGUI>(true);
                    if(okButtonText != null)
                    {
                        okButtonText.text = value;
                    }
                }
            }
        }

        public string CancelText
        {
            set
            {
                if (m_cancelButton != null)
                {
                    TextMeshProUGUI cancelButtonText = m_cancelButton.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (cancelButtonText != null)
                    {
                        cancelButtonText.text = value;
                    }
                }
            }
        }

        public bool IsInteractable
        {
            get { return IsOkInteractable || IsCancelInteractable || IsCloseButtonInteractable; }
            set
            {
                IsOkInteractable = true;
                IsCancelInteractable = true;
                IsCloseButtonInteractable = true;
            }
        }
      
  
        public bool IsOkInteractable
        {
            get
            {
                if(m_okButton == null)
                {
                    return false;
                }

                return m_okButton.interactable;
            }
            set
            {
                if(m_okButton != null)
                {
                    m_okButton.interactable = value;
                }
            }
        }

        public bool IsOkVisible
        {
            set
            {
                if(m_okButton != null)
                {
                    m_okButton.gameObject.SetActive(value);
                }

                if(m_buttonsRoot != null)
                {
                    m_buttonsRoot.gameObject.SetActive(m_cancelButton != null && m_cancelButton.gameObject.activeSelf || m_okButton != null && m_okButton.gameObject.activeSelf);
                }

                if(m_okButton != null)
                {
                    if (value)
                    {
                        m_okButton.Select();
                    }
                }
            }
        }

        public bool IsCancelInteractable
        {
            get
            {
                if(m_cancelButton == null)
                {
                    return false;
                }

                return m_cancelButton.interactable;
            }
            set
            {
                if(m_cancelButton != null)
                {
                    m_cancelButton.interactable = value;
                }
            }
        }

        public bool IsCancelVisible
        {
            set
            {
                if(m_cancelButton != null)
                {
                    m_cancelButton.gameObject.SetActive(value);
                }

                if (m_buttonsRoot != null)
                {
                    m_buttonsRoot.gameObject.SetActive(m_cancelButton != null && m_cancelButton.gameObject.activeSelf || m_okButton != null && m_okButton.gameObject.activeSelf);
                }
            }
        }

        public bool IsCloseButtonInteractable
        {
            get
            {
                if(m_closeButton == null)
                {
                    return false;
                }

                return m_closeButton.interactable;
            }
            set
            {
                if(m_closeButton != null)
                {
                    m_closeButton.interactable = value;
                }
            }
        }

        public bool IsCloseButtonVisible
        {
            set
            {
                if(m_closeButton != null)
                {
                    m_closeButton.gameObject.SetActive(value);
                }
            }
        }

        private void Start()
        {
            if(m_okButton != null)
            {
                m_okButton.onClick.AddListener(OnOkClick);
            }

            if(m_cancelButton != null)
            {
                m_cancelButton.onClick.AddListener(OnCancelClick);
            }

            if(m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(OnCancelClick);
            }

            m_parentRegion = GetComponentInParent<Region>();

            if (m_okButton != null && m_okButton.gameObject.activeSelf)
            {
                m_okButton.Select();
            }
        }

        private void OnOkClick()
        {
            Close(true);
        }

        private void OnCancelClick()
        {
            Close(false);
        }     
        
        public void Hide()
        {
            if(m_parentRegion == null)
            {
                m_parentRegion = GetComponentInParent<Region>();
            }

            m_parentRegion.gameObject.SetActive(false);
        }

        public void Show()
        {
            if(m_parentRegion != null)
            {
                m_parentRegion.gameObject.SetActive(true);
            }
        }

        public void Close(bool? result = null, bool raiseEvents = true, bool invokeActions = true)
        {
            if(m_parentRegion == null)
            {
                Debug.LogWarning("m_parentRegion == null");
                return;
            }


            if(result != null)
            {
                if (result == false)
                {
                    if(Cancel != null && raiseEvents)
                    {
                        DialogCancelArgs args = new DialogCancelArgs();
                        Cancel(this, args);
                        if (args.Cancel)
                        {
                            return;
                        }
                    }

                    if(CancelAction != null && invokeActions)
                    {
                        DialogCancelArgs args = new DialogCancelArgs();
                        CancelAction(this, args);
                        if (args.Cancel)
                        {
                            return;
                        }
                    }
                }
                else if (result == true)
                {
                    if(Ok != null && raiseEvents)
                    {
                        DialogCancelArgs args = new DialogCancelArgs();
                        Ok(this, args);
                        if (args.Cancel)
                        {
                            return;
                        }
                    }
                
                    if(OkAction != null && invokeActions)
                    {
                        DialogCancelArgs args = new DialogCancelArgs();
                        OkAction(this, args);
                        if (args.Cancel)
                        {
                            return;
                        }
                    }
                }
            }

            Destroy(m_parentRegion.gameObject);
            if(Closed != null)
            {
                Closed(this, result);
            }
        }
    }
}
