using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class ClickDebugger : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            var results = new List<RaycastResult>();
            var data = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };
            EventSystem.current.RaycastAll(data, results);

            Debug.Log($"=== CLICK tại {Mouse.current.position.ReadValue()} | Hit {results.Count} objects ===");
            foreach (var r in results)
                Debug.Log($"  → [{r.index}] {r.gameObject.name} | depth={r.depth}");
        }
    }
}