using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class HidePlaceHolderOnClick : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private GameObject placeholder;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (placeholder != null)
            placeholder.SetActive(false);
    }

    private void Update()
    {
        if (!inputField.isFocused && string.IsNullOrEmpty(inputField.text))
        {
            placeholder.SetActive(true);
        }
    }
}
