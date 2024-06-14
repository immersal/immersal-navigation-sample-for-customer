/*===============================================================================
Copyright (C) 2024 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sales@immersal.com for licensing requests.
===============================================================================*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.AI;
using Immersal.XR;
using Immersal.Samples.Util;
using TMPro;

namespace Immersal.Samples.Navigation
{
    [System.Serializable]
    public class NavigationEvent : UnityEvent<Transform>
    {
    }

    public class NavigationManager : MonoBehaviour
    {
        // Navigation Visualization references
        [Header("Visualization")]
        [SerializeField]
        private GameObject m_navigationPathPrefab = null;
        /*
        [SerializeField]
        private GameObject m_navigationArrowPrefab = null;
        */

        // UI Object references
        [Header("UI Objects")]
        [SerializeField]
        private GameObject m_TargetsList = null;
        [SerializeField]
        private Sprite m_ShowListIcon = null;
        [SerializeField]
        private Sprite m_SelectTargetIcon = null;
        [SerializeField]
        private Image m_TargetsListIcon = null;
        [SerializeField]
        private TextMeshProUGUI m_TargetsListText = null;
        [SerializeField]
        private GameObject m_StopNavigationButton = null;

        // new by xy
        [SerializeField]
        private GameObject m_offScreenIndicator = null; 
        [SerializeField]
        private float m_offScreenIndicatorOffset = 0.9f;
        [SerializeField]
        private UIController NavigatingUI;
        public UIController PreNavigatingUI;

        // turn arrow sprite
        public Sprite leftArrow;
        public Sprite rightArrow;
        public Sprite straightArrow;

        [SerializeField]
        private Image m_turnArrow; 
        [SerializeField]
        private TextMeshProUGUI m_distanceText;
        [SerializeField]
        private TextMeshProUGUI m_targetText;

        // new
        [Header("Navigation Agent")]
        [SerializeField]
        private NavigationAgentController m_agentController = null;

        // Navigation Settings
        private enum NavigationMode { NavMesh, Graph};
        [Header("Settings")]
        [SerializeField]
        private NavigationMode m_navigationMode = NavigationMode.NavMesh;
        public bool inEditMode = false;
        /*
        [SerializeField]
        private bool m_showArrow = true;
        */
        [SerializeField]
        private float m_ArrivedDistanceThreshold = 1.0f;
        [SerializeField]
        private float m_pathWidth = 0.3f;
        [SerializeField]
        private float m_heightOffset = 0f; // 0.5f

        // Navigation State Events
        [Header("Events")]
        [SerializeField]
        private NavigationEvent onTargetFound = new NavigationEvent();
        [SerializeField]
        private NavigationEvent onTargetNotFound = new NavigationEvent();

        private XRSpace m_XRSpace = null;
        private bool m_managerInitialized = false;
        private bool m_navigationActive = false;
        private Transform m_targetTransform = null;
        private IsNavigationTarget m_NavigationTarget = null;
        private Transform m_playerTransform = null;
        private GameObject m_navigationPathObject = null;
        private NavigationPath m_navigationPath = null;

        [SerializeField]
        private NavigationGraphManager m_NavigationGraphManager = null;

        private enum NavigationState { NotNavigating, Navigating};
        private NavigationState m_navigationState = NavigationState.NotNavigating;

        private static NavigationManager instance = null;
        public static NavigationManager Instance
        {
            get
            {
#if UNITY_EDITOR
                if (instance == null && !Application.isPlaying)
                {
                    instance = FindObjectOfType<NavigationManager>();
                }
#endif
                if (instance == null)
                {
                    Debug.LogError("No NavigationManager instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }

        public bool navigationActive
        {
            get { return m_navigationActive; }
        }

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            if (instance != this)
            {
                Debug.LogError("NavigationManager: There must be only one NavigationManager in a scene.");
                UnityEngine.Object.DestroyImmediate(this);
                return;
            }
        }

        private void Start()
        {
            InitializeNavigationManager();

            // NavigatingUI.SetAlpha(1); //
            // PreNavigatingUI.SetAlpha(1); // 

            if (m_managerInitialized)
            {
                // PreNavigatingUI.SetAlpha(1);
                m_TargetsListIcon.sprite = m_ShowListIcon;
                m_TargetsListText.text = "Show Navigation Targets";
            }
        }

        private void Update()
        {
            if(m_managerInitialized && m_navigationState == NavigationState.Navigating)
            {
                TryToFindPath(m_NavigationTarget);
            }
        }

        public void InitializeNavigation(NavigationTargetListButton button)
        {
            if (!m_managerInitialized)
            {
                Debug.LogWarning("NavigationManager: Navigation Manager not properly initialized.");
                return;
            }

            m_targetTransform = button.targetObject.transform;
            m_NavigationTarget = button.targetObject.GetComponent<IsNavigationTarget>();
            TryToFindPath(m_NavigationTarget);
            ControlAgent(m_NavigationTarget);
        }

        public void ControlAgent(IsNavigationTarget navigationTarget)
        {
            m_agentController.SetAgent(); 
            if (m_agentController != null)
            {
                // Debug.Log("moving to "+navigationTarget.targetName);
                m_agentController.MoveToPosition(navigationTarget.position);
            } 
            else 
            {
                Debug.LogWarning("NavigationManager: No NavigationAgentController found in the scene.");
            }

        }

        public void TryToFindPath(IsNavigationTarget navigationTarget)
        {
            List<Vector3> corners;

            // Convert to Unity's world space coordinates to use NavMesh
            Vector3 startPosition = m_playerTransform.position;
            Vector3 targetPosition = navigationTarget.position;

            Vector3 delta = targetPosition - startPosition;
            float distanceToTarget = new Vector3(delta.x, delta.y, delta.z).magnitude;

            if (distanceToTarget < m_ArrivedDistanceThreshold)
            {
                m_navigationActive = false;

                m_navigationState = NavigationState.NotNavigating;
                UpdateNavigationUI(m_navigationState);

                DisplayArrivedNotification();
                return;
            }

            switch (m_navigationMode)
            {
                case NavigationMode.NavMesh:

                    m_targetText.text = $"Going to {navigationTarget.targetName}"; //

                    startPosition = XRSpaceToUnity(m_XRSpace.transform, m_XRSpace.InitialPose, startPosition);
                    targetPosition = XRSpaceToUnity(m_XRSpace.transform, m_XRSpace.InitialPose, targetPosition);

                    corners = FindPathNavMesh(startPosition, targetPosition);
                    if (corners.Count >= 2)
                    {
                        m_navigationActive = true;

                        m_navigationState = NavigationState.Navigating;
                        UpdateNavigationUI(m_navigationState);

                        UpdateNavigationUI2(corners); // corners used for waypoint navigation

                        m_navigationPath.GeneratePath(corners, m_XRSpace.transform.up);
                        m_navigationPath.pathWidth = m_pathWidth;
                    }
                    else
                    {
                        NotificationManager.Instance.GenerateNotification("Path to target not found.");
                        // m_agentController.HideAgent();
                        m_navigationState = NavigationState.NotNavigating; // new xy
                        UpdateNavigationUI(m_navigationState);
                    }
                    break;

                case NavigationMode.Graph:

                    corners = m_NavigationGraphManager.FindPath(startPosition, targetPosition);

                    if (corners.Count >= 2)
                    {
                        m_navigationActive = true;

                        m_navigationState = NavigationState.Navigating;
                        UpdateNavigationUI(m_navigationState);

                        m_navigationPath.GeneratePath(corners, m_XRSpace.transform.up);
                        m_navigationPath.pathWidth = m_pathWidth;
                    }
                    else
                    {
                        NotificationManager.Instance.GenerateNotification("Path to target not found.");
                        UpdateNavigationUI(m_navigationState);  // ??
                    }
                    break;
            }

            UpdateOffScreenIndicator(navigationTarget);
        }


        private void UpdateOffScreenIndicator(IsNavigationTarget navigationTarget)
        {
            // off-screen indicator
            // reference: https://assetstore.unity.com/packages/tools/gui/off-screen-target-indicator-71799?locale=zh-CN

            Vector3 screenPoint = Camera.main.WorldToScreenPoint(navigationTarget.position); 

            bool isTargetVisible = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < Screen.width && screenPoint.y > 0 && screenPoint.y < Screen.height;
            
            if (!isTargetVisible) // target off screen
            {
                m_offScreenIndicator.SetActive(true);

                Vector3 screenCentre = new Vector3(Screen.width, Screen.height, 0) / 2;   
                Vector3 screenBounds = screenCentre * m_offScreenIndicatorOffset;
                screenPoint -= screenCentre; // set screen point relative to the center for computational convenience

                if(screenPoint.z < 0)
                {
                    screenPoint *= -1;
                }

                float angle = Mathf.Atan2(screenPoint.y, screenPoint.x);
                float slope = Mathf.Tan(angle);

                if(screenPoint.x > 0)
                {
                    screenPoint = new Vector3(screenBounds.x, screenBounds.x * slope, 0);
                }
                else
                {
                    screenPoint = new Vector3(-screenBounds.x, -screenBounds.x * slope, 0);
                } 
                if(screenPoint.y > screenBounds.y)
                {
                    screenPoint = new Vector3(screenBounds.y / slope, screenBounds.y-360, 0); // avoid overlapping buttons/icons
                }
                else if(screenPoint.y < -screenBounds.y)
                {
                    screenPoint = new Vector3(-screenBounds.y / slope, -screenBounds.y+128, 0);
                }

                screenPoint += screenCentre; // bring the ScreenPoint back to its original reference

                m_offScreenIndicator.transform.position = new Vector3 (screenPoint.x, screenPoint.y, 0.8f);
                m_offScreenIndicator.transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg); 
            }
            else // target on screen
            {
                if (m_offScreenIndicator != null)
                {
                    m_offScreenIndicator.SetActive(false);
                }
            }
        }


        private List<Vector3> FindPathNavMesh(Vector3 startPosition, Vector3 targetPosition)
        {
            NavMeshPath path = new NavMeshPath();
            List<Vector3> collapsedCorners = new List<Vector3>();

            if (NavMesh.CalculatePath(startPosition, targetPosition, NavMesh.AllAreas, path))
            {
                List<Vector3> corners = new List<Vector3>(path.corners);

                for (int i = 0; i < corners.Count; i++)
                {
                    corners[i] = corners[i] + new Vector3(0f, m_heightOffset, 0f);
                    corners[i] = UnityToXRSpace(m_XRSpace.transform, m_XRSpace.InitialPose, corners[i]);
                }

                for (int i = 0; i < corners.Count - 1; i++)
                {
                    Vector3 currentPoint = corners[i];
                    Vector3 nextPoint = corners[i + 1];
                    float threshold = 0.75f;

                    if (Vector3.Distance(currentPoint, nextPoint) > threshold)
                    {
                        collapsedCorners.Add(currentPoint);
                    }
                }

                collapsedCorners.Add(corners[corners.Count - 1]);
            }

            return collapsedCorners;
        }

        public void ToggleTargetsList()
        {
            if (!m_managerInitialized)
            {
                Debug.LogWarning("NavigationManager: Navigation Manager not properly initialized.");
                return;
            }

            if (m_TargetsList.activeInHierarchy)
            {
                m_TargetsList.SetActive(false);
                if (m_ShowListIcon != null && m_TargetsListIcon != null)
                {
                    m_TargetsListIcon.sprite = m_ShowListIcon;
                }
                if (m_TargetsListText != null)
                {
                    m_TargetsListText.text = "Show Navigation Targets";
                }
            }
            else
            {
                m_TargetsList.SetActive(true);
                m_TargetsList.GetComponent<NavigationTargetListControl>().GenerateButtons();
                if (m_SelectTargetIcon != null && m_TargetsListIcon != null)
                {
                    m_TargetsListIcon.sprite = m_SelectTargetIcon;
                }
                if (m_TargetsListText != null)
                {
                    m_TargetsListText.text = "Select Navigation Target";
                }
            }
        }

        public void ToggleEditMode()
        {
            inEditMode = !inEditMode;
        }

        public void DisplayPathNotFoundNotification()
        {
#if !(UNITY_STANDALONE)
            Handheld.Vibrate();
#endif
            NotificationManager.Instance.GenerateNotification("Path to target could not be found.");
            onTargetNotFound.Invoke(m_targetTransform);
        }

        public void DisplayArrivedNotification()
        {
#if !(UNITY_STANDALONE)
            Handheld.Vibrate();
#endif
            NotificationManager.Instance.GenerateNotification("You have arrived!");
            onTargetFound.Invoke(m_targetTransform);
        }

        public void StopNavigation()
        {
            m_navigationActive = false;

            m_navigationState = NavigationState.NotNavigating;
            UpdateNavigationUI(m_navigationState);

            NotificationManager.Instance.GenerateNotification("Navigation stopped.");
        }

        private void UpdateNavigationUI(NavigationState navigationState)
        {
            switch(navigationState)
            {
                case NavigationState.NotNavigating:
                    // m_StopNavigationButton.SetActive(false);
                    m_navigationPathObject.SetActive(false);

                    // m_offScreenIndicator.SetActive(false);
                    // m_turnArrow.gameObject.SetActive(false);
                    // m_distanceText.gameObject.SetActive(false);

                    PreNavigatingUI.FadeIn(0.5f, 1f);
                    NavigatingUI.FadeOut(0.5f);

                    // m_agentController.DisplayMessage("You have arrived at your destination!");
                    m_agentController.HideAgent();
                    
                    break;
                case NavigationState.Navigating:
                    // m_StopNavigationButton.SetActive(true);
                    m_navigationPathObject.SetActive(true);

                    // m_turnArrow.gameObject.SetActive(true);
                    // m_distanceText.gameObject.SetActive(true);

                    PreNavigatingUI.FadeOut(0.5f);
                    NavigatingUI.FadeIn(0.5f, 1f);
                    break;
            }
        }

        private void UpdateNavigationUI2(List<Vector3> pathCorners) // Calculate the next turn and distance to the next turn
        {    

            if (pathCorners.Count > 1)
            {
                m_turnArrow.enabled = true;

                // Vector3 currentDirection = Camera.main.transform.forward.normalized;
                Vector3 currentDirection = (pathCorners[1] - pathCorners[0]).normalized;
                float distanceToNextTurn = Vector3.Distance(pathCorners[0], pathCorners[1]);

                string turnDirection = DetermineTurnDirection(currentDirection, pathCorners);

                if (turnDirection == "Left")
                {
                    m_turnArrow.sprite = leftArrow;
                    m_distanceText.text = $"Turn left in {distanceToNextTurn:F1}m";
                }
                else if (turnDirection == "Right")
                {
                    m_turnArrow.sprite = rightArrow;
                    // m_turnArrow.transform.rotation = Quaternion.Euler(0, 0, 180); // Rotate arrow to right
                    m_distanceText.text = $"Turn right in {distanceToNextTurn:F1}m";
                } else {
                    m_turnArrow.sprite = straightArrow;
                    // m_turnArrow.transform.rotation = Quaternion.Euler(0, 0, 90); // Rotate arrow to forward
                    m_distanceText.text = $"Go straight {distanceToNextTurn:F1}m";
                }
            }
        }

        private string DetermineTurnDirection(Vector3 currentDirection, List<Vector3> pathCorners)
        {
            if (pathCorners.Count <= 2)
            {
                return "Straight";
            }

            Vector3 nextDirection = (pathCorners[2] - pathCorners[1]).normalized;

            Vector2 currentDir2D = new Vector2(currentDirection.x, currentDirection.z); // project directions onto the XZ plane
            Vector2 nextDir2D = new Vector2(nextDirection.x, nextDirection.z);

            float determinant = currentDir2D.x * nextDir2D.y - currentDir2D.y * nextDir2D.x; // 2D cross product

            Debug.Log($"Current Direction: {currentDirection}, Next Direction: {nextDirection}, Determinant: {determinant}");

            float threshold = 0.01f; // small threshold to avoid small deviations causing incorrect turn directions

            if (determinant > threshold)
            {
                return "Left";
            }
            else if (determinant < -threshold)
            {
                return "Right";
            }
            else
            {
                return "Straight";
            }
        }

        private void InitializeNavigationManager()
        {
            if (m_XRSpace == null)
            {
                m_XRSpace = FindObjectOfType<XRSpace>();

                if (m_XRSpace == null)
                {
                    Debug.LogWarning("NavigationManager: No XR Space found in scene, ensure one exists.");
                    return;
                }
            }

            m_NavigationGraphManager = GetComponent<NavigationGraphManager>();
            if (m_NavigationGraphManager == null)
            {
                Debug.LogWarning("NavigationManager: Missing Navigation Graph Manager component.");
                return;
            }

            m_playerTransform = Camera.main.transform;
            if (m_playerTransform == null)
            {
                Debug.LogWarning("NavigationManager: Could not find the main camera. Do you have the MainCamera tag applied?");
                return;
            }

            if (m_navigationPathPrefab == null)
            {
                Debug.LogWarning("NavigationManager: Missing navigation path object reference.");
                return;
            }

            if(m_navigationPathPrefab != null)
            {
                if (m_navigationPathObject == null)
                {
                    m_navigationPathObject = Instantiate(m_navigationPathPrefab);
                    m_navigationPathObject.SetActive(false);
                    m_navigationPath = m_navigationPathObject.GetComponent<NavigationPath>();
                }

                if(m_navigationPath == null)
                {
                    Debug.LogWarning("NavigationManager: NavigationPath component in Navigation path is missing.");
                    return;
                }
            }

            if (m_TargetsList == null)
            {
                Debug.LogWarning("NavigationManager: Navigation Targets List reference is missing.");
                return;
            }

            if (m_ShowListIcon == null)
            {
                Debug.LogWarning("NavigationManager: \"Show List\" icon is missing.");
                return;
            }

            if (m_SelectTargetIcon == null)
            {
                Debug.LogWarning("NavigationManager: \"Select Target\" icon is missing.");
                return;
            }

            if (m_TargetsListIcon == null)
            {
                Debug.LogWarning("NavigationManager: \"Targets List\" icon reference is missing.");
                return;
            }

            if (m_TargetsListText == null)
            {
                Debug.LogWarning("NavigationManager: \"Targets List\" text reference is missing.");
                return;
            }

            if (m_StopNavigationButton == null)
            {
                Debug.LogWarning("NavigationManager: Stop Navigation Button reference is missing.");
                return;
            }

            m_managerInitialized = true;
        }

        private Vector3 XRSpaceToUnity(Transform XRSpace, Matrix4x4 XRSpaceOffset, Vector3 pos)
        {
            Matrix4x4 m = XRSpace.worldToLocalMatrix;
            pos = m.MultiplyPoint(pos);
            pos = XRSpaceOffset.MultiplyPoint(pos);
            return pos;
        }

        private Vector3 XRSpaceToUnity(Transform XRSpace, Vector3 pos)
        {
            pos = XRSpaceToUnity(XRSpace, Matrix4x4.identity, pos);
            return pos;
        }

        private Vector3 UnityToXRSpace(Transform XRSpace, Matrix4x4 XRSpaceOffset, Vector3 pos)
        {
            pos = XRSpaceOffset.inverse.MultiplyPoint(pos);
            Matrix4x4 m = XRSpace.localToWorldMatrix;
            pos = m.MultiplyPoint(pos);
            return pos;
        }

        private Vector3 UnityToXRSpace(Transform XRSpace, Vector3 pos)
        {
            pos = UnityToXRSpace(XRSpace, Matrix4x4.identity, pos);
            return pos;
        }

    }
}