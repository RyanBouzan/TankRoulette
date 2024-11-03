using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

namespace TankRoulette
{
    public class ProgressBar : NetworkBehaviour
    {
        [SerializeField]
        private ObjectiveObject objective;

        [SerializeField]
        private GameRoundManager GRM;

        [SerializeField]
        private Image _progressbar;

        [SerializeField]
        private bool checking = false;

        public override void OnStartClient()
        {
            base.OnStartClient();

            //enable only for local client
            if (!base.IsOwner)
            {
                this.enabled = false;
            }

            //get references to game round manager and the progress bar image
            _progressbar = GetComponent<Image>();

        }

        void Update()
        {
            if (GRM == null)
            {
                try
                {
                    GRM = GameObject.Find("GameManager").GetComponent<GameRoundManager>();
                }
                catch (NullReferenceException)
                {
                    Debug.Log("progress bar waiting for GameManager");
                    return;
                }

            }
            //Debug.LogWarning(GRM.gameObject.name);

            //if there is an active objective, get the object by its tag and find its script component
            //we need this to check the objective's capture progress and see if it is captured
            if (GRM.ObjectiveIsActive)
            {
                objective = GameObject.FindGameObjectWithTag("Objective").GetComponent<ObjectiveObject>();
            }


            //sanity check to make sure there is an object for the reference
            if (objective != null)
            {

                //checker that is based on a 2 second delay that determines whether or not
                //to show the progress bar
                if (!checking)
                {
                    StartCoroutine(CheckForProgress(objective.CaptureProgress));
                }

                //if the obj is finished (as in about to dissapear), set the reference to null
                //and make the bar red and hide it again
                if (objective.finished)
                {
                    _progressbar.enabled = false;
                    _progressbar.color = Color.red;
                    objective = null;
                }

                //the fill amount is a float from 0, 1.
                //it is calculated from capture progress/full capture, where full capture is the max, and 0 is the min.
                _progressbar.fillAmount = Mathf.InverseLerp(0f, objective.FullCapture, objective.CaptureProgress);

                //captured means display the captured state/animation for 10 seconds
                //in this case, make the bar green
                if (objective.captured)
                {
                    _progressbar.color = Color.green;
                }
            }

        }

        IEnumerator CheckForProgress(int oldVal)
        {
            yield return new WaitForSeconds(2f);
            //if the value has changed then show the bar
            if (oldVal != objective.CaptureProgress)
            {
                //Debug.LogError("showing progress bar");
                _progressbar.enabled = true;
            }
            else
            //if the progress has not changed
            {
                //if the objective is not captured, hide the bar.
                //we want the bar to show green at the end, which implies that
                //the progress is not changing, so we still 
                if (!objective.captured)
                    _progressbar.enabled = false;
            }
            //reset checking bool because 2 second wait
            checking = false;

        }


    }
}