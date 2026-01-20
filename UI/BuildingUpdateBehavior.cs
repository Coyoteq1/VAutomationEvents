using System;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace VAuto.UI
{
    public class BuildingUpdateBehavior : MonoBehaviour
    {
        private static BuildingUpdateBehavior _instance;
        private static GameObject _obj;

        public static void Setup()
        {
            if (_instance != null) return;

            ClassInjector.RegisterTypeInIl2Cpp<BuildingUpdateBehavior>();
            _obj = new GameObject("VAuto_BuildingUpdate");
            UnityEngine.Object.DontDestroyOnLoad(_obj);
            _obj.hideFlags = HideFlags.HideAndDontSave;
            _instance = _obj.AddComponent<BuildingUpdateBehavior>();
        }

        public Action OnUpdate;

        private void Update()
        {
            OnUpdate?.Invoke();
        }

        public static void Cleanup()
        {
            if (_obj != null)
            {
                Destroy(_obj);
                _obj = null;
                _instance = null;
            }
        }
    }
}
