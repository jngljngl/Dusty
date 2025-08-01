using UnityEngine;

public class Flashlight : MonoBehaviour
{


    [SerializeField] private Transform mainCamera;
    // [SerializeField] private Light flashLight;
    private Light flashLight;


    private float flashlighIntesity = 100f;
    [Range(0f, 10f)]
    [SerializeField] private float batteryCount = 4f;
    [SerializeField] private float batteryDuration = 5f;
     private float elapsedTime;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
        flashLight  = GetComponent<Light>();
        flashLight.enabled = false;
        
    }

    // Update is called once per frame
    void Update()
    {
       
        CameraRotation();
        //FlashlightDecrease();




    }


    private void FlashlightDecrease()
    {

        elapsedTime += Time.deltaTime;
        float startingIntensity = flashLight.intensity;

        if (elapsedTime > batteryDuration) {

            float flashLightDuration = flashLight.intensity / batteryDuration;
            flashLight.intensity -= flashLightDuration;

        }

        if (batteryCount > 0)
        {

            batteryCount -= 1f;
            flashLight.enabled = true;
            flashLight.intensity = startingIntensity;


        }
        else {

            flashLight.enabled = false;

        }




    }

    public void Toggle() {


        flashLight.enabled = !flashLight.enabled;

        
    }

    private void CameraRotation() { 
    
        this.transform.rotation = mainCamera.transform.rotation;
    
    }
}
