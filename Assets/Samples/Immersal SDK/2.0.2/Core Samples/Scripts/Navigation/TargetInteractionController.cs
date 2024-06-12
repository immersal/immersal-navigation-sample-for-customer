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
        [SerializeField] private float popupDuration = 6.0f;
        [SerializeField] private float heightOffset = 0.5f;

        void Start()
        {
            if (popup != null)
            {
                Vector3 popupPosition = transform.position + new Vector3(0, heightOffset, 0);
                popup.transform.position = popupPosition;
                popup.text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
            }
            else
            {
                Debug.LogWarning("Popup does not exist.");
            }
            popup.gameObject.SetActive(false);
        }

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
                }
            }
        }

        private void TogglePopup()
        {
            // isPopupVisible = !isPopupVisible;
            popup.gameObject.SetActive(true);
            StartCoroutine(HidePopupAfterDelay(popupDuration));
        }

        private IEnumerator HidePopupAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            popup.gameObject.SetActive(false);
        }

    }
}