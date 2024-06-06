using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

namespace Immersal.Samples.Navigation
{
    public class NavigationAgentController : MonoBehaviour
    {
        public NavMeshAgent agent;
        [SerializeField] private float startDistance = 2.0f; // distance in front of the camera
        [SerializeField] private TextMeshPro messageText = null;

        public Animator animator;
        [SerializeField] private float greetingDuration = 4.0f;
        private bool isMoving = false;

        [SerializeField] private float maxDistance = 5f;     // Maximum allowed distance between user and agent
        [SerializeField] private float speedFactor = 1.5f;   // Factor to control agent's speed relative to user's speed
        [SerializeField] private float userStopThreshold = 0.1f; // Threshold to consider the user as stopped
        [SerializeField] private float stopDelay = 1f;       // Delay before stopping the agent when user stops

        private Vector3 previousCameraPosition; // for user (main camera) movement tracking
        private float userSpeed;
        private float stopTimer;


        private void Start()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            agent.gameObject.SetActive(false);

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            previousCameraPosition = Camera.main.transform.position;
        }


        public void MoveToPosition(Vector3 position)
        {
            agent.gameObject.SetActive(true);

            animator.SetTrigger("Greet");
            DisplayMessage("Welcome");

            StartCoroutine(WaitAndMoveToTarget(position));
        }

        IEnumerator WaitAndMoveToTarget(Vector3 position)
        {
            yield return new WaitForSeconds(greetingDuration);
            MoveToTarget(position);
        }

        private void MoveToTarget(Vector3 position)
        {   
            animator.SetTrigger("Move");
            agent.SetDestination(position);
            DisplayMessage("Follow me");
            isMoving = true;
        }


        private void Update()
        {
            if (isMoving && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    ArriveAtTarget();
                }
            }

            MonitorAgentAndUser();
        }

        private void ArriveAtTarget()
        {
            isMoving = false;
            animator.SetTrigger("Greet");
            DisplayMessage("Arrived");
            Invoke("HideAgent", greetingDuration);
        }

        public void HideAgent() 
        {
            agent.gameObject.SetActive(false);
            isMoving = false;
        }


        public void SetAgent()
        {
            isMoving = false;
            agent.gameObject.SetActive(false);
            Vector3 startPosition = Camera.main.transform.position + Camera.main.transform.forward * startDistance;
            agent.Warp(startPosition);
            agent.transform.rotation = Camera.main.transform.rotation;

            // messageText.transform.rotation = Quaternion.Euler(0, 180, 0); 
            // agent.transform.rotation = Quaternion.Euler(0, 180, 0);
        }


        private void MonitorAgentAndUser()
        {
            Vector3 currentCameraPosition = Camera.main.transform.position;
            float distance = Vector3.Distance(currentCameraPosition, agent.transform.position); // distance between user and agent
            float positionChange = Vector3.Distance(previousCameraPosition, currentCameraPosition); // user movement
            float userSpeed = (currentCameraPosition - previousCameraPosition).magnitude / Time.deltaTime;

            if (distance > maxDistance)
            {
                if (userSpeed > userStopThreshold) // if the user is moving, adjust agent speed
                {
                    agent.speed = userSpeed * speedFactor;
                    stopTimer = 0f; 
                }
                else // if the user is stopped
                {
                    stopTimer += Time.deltaTime;
                    if (stopTimer >= stopDelay)
                    {
                        agent.isStopped = true;
                        animator.SetTrigger("Greet");
                        DisplayMessage("Keep moving");
                    }
                    else
                    {
                        agent.isStopped = false;
                        animator.SetTrigger("Move");
                        DisplayMessage("Follow me");
                    }
                }
            }            

            previousCameraPosition = currentCameraPosition;

        }


        public void DisplayMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

    }
}