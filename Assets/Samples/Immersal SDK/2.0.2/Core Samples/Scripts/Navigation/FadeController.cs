using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Immersal.XR;

namespace Immersal.Samples.Navigation
{

    public class FadeController: MonoBehaviour
    {
        public UIController OnboardingUI;
        public UIController NavUI;
        public UIController PreNavigatingUI;

        void Start()
        {
            NavUI.SetAlpha(0);
            OnboardingUI.SetAlpha(0.8f);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                OnboardingUI.FadeOut(1); 
                NavUI.FadeIn(0.8f);
                PreNavigatingUI.SetAlpha(1);
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                OnboardingUI.FadeIn(1); 
                NavUI.FadeOut(0.8f);
            }
        }

        public void showInstruction()
        {
            OnboardingUI.FadeIn(1); 
            NavUI.FadeOut(0.8f);
            PreNavigatingUI.SetAlpha(1);
        }

        public void hideInstruction()
        {
            OnboardingUI.FadeOut(1); 
            NavUI.FadeIn(0.8f);
        }
    }

}
