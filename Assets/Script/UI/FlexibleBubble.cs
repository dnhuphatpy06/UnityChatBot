using UnityEngine;
using UnityEngine.UI;
using TMPro;
[ExecuteAlways]
public class FlexibleBubble : MonoBehaviour
{
    [SerializeField] private float maxWidth = 800f; // Canvas × 0.70

    private LayoutElement _le;
    private TMP_Text _tmp;

    void Awake()
    {
        _le = GetComponent<LayoutElement>();
        _tmp = GetComponentInChildren<TMP_Text>();
    }

    void Update()
    {
        if (_tmp == null || _le == null) return;

        float textNaturalWidth = _tmp.preferredWidth + 24f; // +24 = padding L+R

        _le.preferredWidth = Mathf.Min(textNaturalWidth, maxWidth);
    }
}
