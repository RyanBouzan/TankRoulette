using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingProgress : MonoBehaviour
{
    [SerializeField] private Slider slider;
    // Start is called before the first frame update

    public float targetFill = 0f;

    public void StartLoading(float time)
    {
        slider.value = 0f;
        StartCoroutine(IncrementSlider(time));
    }

    IEnumerator IncrementSlider(float delay)
    {
        float elapsedTime = 0f;
        float startFill = 0f;

        while (elapsedTime < delay)
        {
            float fillAmount = Mathf.Lerp(startFill, targetFill, elapsedTime / delay);
            slider.value = fillAmount;

            elapsedTime += Time.fixedDeltaTime;
            yield return null;
        }
        //yield return new WaitForSeconds(delay);  //wait
        // Ensure the final fill amount is exactly the target fill
        slider.value = targetFill;
        transform.root.GetComponent<Animator>().SetTrigger("Trigger");
        yield return new WaitForSeconds(2.5f);
        transform.root.gameObject.SetActive(false);

        //EndReload(this);  //can fire
    }
}
