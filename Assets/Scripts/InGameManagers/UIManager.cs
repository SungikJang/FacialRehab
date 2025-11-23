using System;
using System.Collections.Generic;
using Specifications;
using UnityEngine;

namespace InGameManagers
{
    public interface IUIManager
    {
        void Init();
        void ShowDefaultPopup(UIType uiType);
        // void ShowPopUpUI(string name);
        // void ClosePopUpUI(string name);
        // void DeletePopUpUI(string name);
        // void ClearAllPopUps();
        // void ShowAlertUI(string name);
        // void DeleteTargetAlertUI(string name);
        // void ClearAllAlertUIs();
    }
    
    [Injectable(typeof(IUIManager), ServiceLifetime.Singleton)]
    public class UIManager: IUIManager
    {
        private Dictionary<UIType, GameObject> _defaultUICollection;
        public UIManager()
        {
        }
        public void Init()
        {
            InitializeDefaultUI();
        }

        public void ShowDefaultPopup(UIType uiType)
        {
            foreach (var defaultUI in _defaultUICollection)
            {
                if (defaultUI.Key == uiType)
                {
                    defaultUI.Value.SetActive(true);
                }
                else
                {
                    defaultUI.Value.SetActive(false);
                }
            }
        }

        private void InitializeDefaultUI()
        {
            GameObject uiGroup = GameObject.Find("UIGroup");
            foreach (UIType type in Enum.GetValues(typeof(UIType)))
            {
                string uiName = type.ToString();

                GameObject foundObject = null;
                foundObject = GameObject.Find(uiName);
                if (foundObject != null)
                {
                    _defaultUICollection.Add(type, foundObject);
                    Debug.Log($"찾음: {foundObject.name}");
                }
                else
                {
                    Debug.LogWarning($"{uiName} 이름의 오브젝트를 찾지 못함");
                }
            }
        }
    }
}