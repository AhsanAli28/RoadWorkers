using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.MainMenu.UI
{
    public class ConfigItemBase : MonoBehaviour
    {
        private List<Func<bool>> showCallbacks;
        private bool orCondition;

        public bool IsActive => gameObject.activeSelf;

        public bool CanShow
        {
            get
            {
                var canShow = true;

                for (int i = 0; i < showCallbacks?.Count; i++)
                {
                    try
                    {
                        canShow = showCallbacks[i].Invoke();
                    }
                    catch { }

                    if (!orCondition)
                    {
                        if (!canShow)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (canShow)
                        {
                            break;
                        }
                    }
                }

                return canShow;
            }
        }

        public void Initialize(List<Func<bool>> showCallbacks, bool orCondition, bool canShow = true)
        {
            this.showCallbacks = showCallbacks;
            this.orCondition = orCondition;

            if (!canShow)
            {
                SwitchActive(false);
            }
        }

        public void CheckShowState()
        {
            if (IsActive != CanShow)
            {
                SwitchActive(CanShow);
            }
        }

        public void SwitchActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }
    }
}
