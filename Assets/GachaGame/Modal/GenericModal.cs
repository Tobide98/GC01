namespace Gravitons.UI.Modal
{
    using DG.Tweening;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class GenericModal : Modal
    {
        [Tooltip("Modal title")]
        [SerializeField] protected TextMeshProUGUI m_Title;
        [Tooltip("Modal body")]
        [SerializeField] protected TextMeshProUGUI m_Body;
        [Tooltip("Buttons in the modal")]
        [SerializeField] protected Button[] m_Buttons;
        [SerializeField] CanvasGroup canvasGroup;
        Tween showTween;

        /// <summary>
        /// Deactivate buttons in awake
        /// </summary>
        public void Awake()
        {
            for (int i = 0; i < m_Buttons.Length; i++)
            {
                m_Buttons[i].gameObject.SetActive(false);
            }
        }

        public override void Show(ModalContentBase modalContent, ModalButton[] modalButton)
        {
            PlayAnimation();
            GenericModalContent content = (GenericModalContent) modalContent;
            m_Title.text = content.Title;
            m_Body.text = content.Body;
            
            //Activate buttons and populate properties
            for (int i = 0; i < modalButton.Length; i++)
            {
                if (i >= m_Buttons.Length)
                {
                    Debug.LogError($"Maximum number of buttons of this modal is {m_Buttons.Length}. But {modalButton.Length} ModalButton was given. To display all buttons increase the size of the button array to at least {modalButton.Length}");
                    return;
                }
                m_Buttons[i].gameObject.SetActive(true);
                m_Buttons[i].GetComponentInChildren<TextMeshProUGUI>().text = modalButton[i].Text;
                int index = i; //Closure
                m_Buttons[i].onClick.AddListener(() =>
                {
                    if (modalButton[index].Callback != null)
                    {
                        modalButton[index].Callback();
                    }
                    
                    if (modalButton[index].CloseModalOnClick)
                    {
                        Close();
                    }
                    m_Buttons[index].onClick.RemoveAllListeners();
                });
            }
        }

        void PlayAnimation()
        {
            showTween?.Kill();

            var seq = DOTween.Sequence();

            if (canvasGroup != null)
                seq.Join(canvasGroup.DOFade(1f, 0.2f).From(0));

            seq.Join(this.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).From(0.5f));

            showTween = seq;
        }
    }
}