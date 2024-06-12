using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Immersal.Samples.Navigation
{
    public class TargetInteractionController : MonoBehaviour
    {
        [SerializeField] private TextMeshPro popup = null;
        private bool isPopupVisible = false;

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                 if (Physics.Raycast(ray, out hit))
                {
                    if (Vector3.Distance(hit.point, transform.position) <= 1.0f)
                    {
                        TogglePopup();
                    }
                    else if (isPopupVisible)
                    {
                        HidePopup();
                    }
                }
                else if (isPopupVisible)
                {
                    HidePopup();
                }
            }
        }

        private void TogglePopup()
        {
            if (!isPopupVisible)
            {
                ShowPopup();
            }
            else
            {
                HidePopup();
            }
        }

        private void ShowPopup()
        {       
            if (popup != null)
            {
                Vector3 popupPosition = transform.position + new Vector3(0, 0.5f, 0); // Adjust as needed
                popup.transform.position = popupPosition;
                popup.text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
                isPopupVisible = true;
            }
            else
            {
                Debug.LogWarning("Popup does not exist.");
            }    
        }

        private void HidePopup()
        {
            if (popup != null)
            {
                popup.text = "";
                isPopupVisible = false;
            }
            else
            {
                Debug.LogWarning("Popup does not exist.");
            }
        }
    }
}