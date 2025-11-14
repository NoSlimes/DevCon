using UnityEngine;
using UnityEngine.EventSystems;

namespace NoSlimes.Util.DevCon
{
    internal class ConsoleUIResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private RectTransform targetPanel;
        [SerializeField] private float minHeight = 100f;
        [SerializeField] private float maxHeight = 800f;

        private float startTopOffset;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (targetPanel != null)
                startTopOffset = targetPanel.offsetMax.y;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (targetPanel == null) return;

            float newTop = startTopOffset + eventData.delta.y;

            float clampedHeight = Mathf.Clamp(targetPanel.rect.height + eventData.delta.y, minHeight, maxHeight);
            float delta = clampedHeight - targetPanel.rect.height;

            Vector2 offsetMax = targetPanel.offsetMax;
            offsetMax.y += delta;
            targetPanel.offsetMax = offsetMax;
        }
    }
}
