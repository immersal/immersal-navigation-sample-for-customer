using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Immersal.Samples.Navigation
{
    public class DebugToggleController : MonoBehaviour
    {
        public Toggle toggle;
        public GameObject[] prefabs;
        public GameObject[] planes;
        private bool debug = true;

        public void ToggleValueChanged()
        {
            foreach (GameObject prefab in prefabs)
            {
                prefab.SetActive(debug);
            }
            foreach (GameObject plane in planes)
            {
                plane.SetActive(debug);
            }

            debug = !debug;
        }
    }
}