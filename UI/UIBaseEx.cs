using System;
using UnityEngine;

namespace VAuto.UI
{
    /// <summary>
    /// Simple UI base class - minimal implementation
    /// </summary>
    public class UIBaseEx
    {
        private readonly string _id;
        private readonly Action _updateMethod;
        private GameObject _rootObject;
        private bool _isVisible = false;

        public UIBaseEx(string id, Action updateMethod)
        {
            _id = id;
            _updateMethod = updateMethod;
            CreateRootObject();
        }

        private void CreateRootObject()
        {
            _rootObject = new GameObject($"VAuto_UI_{_id}");
            UnityEngine.Object.DontDestroyOnLoad(_rootObject);
        }

        public GameObject GetRootObject() => _rootObject;

        public void Show()
        {
            _isVisible = true;
            _rootObject?.SetActive(true);
            Plugin.Logger?.LogInfo($"[UIBaseEx] Showing UI {_id}");
        }

        public void Hide()
        {
            _isVisible = false;
            _rootObject?.SetActive(false);
            Plugin.Logger?.LogInfo($"[UIBaseEx] Hiding UI {_id}");
        }

        public void Destroy()
        {
            if (_rootObject != null)
            {
                UnityEngine.Object.Destroy(_rootObject);
                _rootObject = null;
                Plugin.Logger?.LogInfo($"[UIBaseEx] Destroyed UI {_id}");
            }
        }
    }
}