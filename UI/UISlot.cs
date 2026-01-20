using System;
using UnityEngine;

namespace VAuto.UI
{
    /// <summary>
    /// Simple UI slot for ability display - minimal implementation
    /// </summary>
    public class UISlot : MonoBehaviour
    {
        public Action OnClick;
        private Rect _bounds;
        private string _text = "";
        
        public void SetBounds(Rect bounds)
        {
            _bounds = bounds;
        }
        
        public void SetText(string text)
        {
            _text = text;
        }
        
        // Placeholder for UI rendering - would need proper Unity UI setup
        public void TriggerClick()
        {
            OnClick?.Invoke();
        }
    }
}