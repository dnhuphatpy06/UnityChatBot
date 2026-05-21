using UnityEngine;
using System.Collections;

namespace ChatApp.UI
{
    public class TypingIndicatorAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform dot1, dot2, dot3;

        private float _bounceHeight = 12f;
        private float _speed = 0.35f;

        private Vector3 _origin1, _origin2, _origin3;

        private void OnEnable()
        {
            // Cache vị trí gốc (đã set thủ công trong Editor)
            _origin1 = dot1.localPosition;
            _origin2 = dot2.localPosition;
            _origin3 = dot3.localPosition;

            StartCoroutine(Animate());
        }

        private void OnDisable()
        {
            StopAllCoroutines();

            // Reset về vị trí gốc
            dot1.localPosition = _origin1;
            dot2.localPosition = _origin2;
            dot3.localPosition = _origin3;
        }

        private IEnumerator Animate()
        {
            while (true)
            {
                yield return BounceDot(dot1, _origin1);
                yield return new WaitForSeconds(0.1f);
                yield return BounceDot(dot2, _origin2);
                yield return new WaitForSeconds(0.1f);
                yield return BounceDot(dot3, _origin3);
                yield return new WaitForSeconds(0.3f);
            }
        }

        private IEnumerator BounceDot(RectTransform dot, Vector3 origin)
        {
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / _speed;
                float y = Mathf.Sin(t * Mathf.PI) * _bounceHeight;
                dot.localPosition = origin + Vector3.up * y;
                yield return null;
            }
            dot.localPosition = origin;
        }
    }
}