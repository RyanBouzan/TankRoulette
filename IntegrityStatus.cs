using UnityEngine;
using UnityEngine.UI;

namespace TankRoulette
{

    //ill have you N this sucker was pooped out by mr chat gpt

    public class IntegrityStatus : MonoBehaviour
    {
        [SerializeField]
        private Image Front; // Reference to the "Front" Image component

        [SerializeField]
        private Image Left; // Reference to the "Left" Image component

        [SerializeField]
        private Image Right; // Reference to the "Right" Image component

        [SerializeField]
        private Image Rear; // Reference to the "Rear" Image component

        [SerializeField]
        private Image Main; // Reference to the "Main" Image component

        [SerializeField]
        private Image EngineIndicator;

        [SerializeField]
        private Image TrophyIndicator;

        [Header("Assigned at runtime")]
        [SerializeField]
        private PlayerDurability activeTankDurability;

        [SerializeField]
        private TankMotor motor;

        [SerializeField] private Color red = Color.red;
        [SerializeField] private Color green = Color.green;
        [SerializeField] private Color yellow = new Color(1f, 1f, 0f); // Yellow is a combination of full red and full green

        private float maxIntegrity;
        [SerializeField]
        private float integrity;
        // Start is called before the first frame update
        void Start()
        {
            /*
            maxIntegrity = transform.root.GetComponent<ClientTankManager>().activeTank.
            GetComponent<PlayerDurability>().defaultIntegrity;
            */
        }

        // Update is called once per frame
        void Update()
        {
            float front = 0f;
            float left = 0f;
            float right = 0f;
            float rear = 0f;
            float main = 0f;
            float engine = 0f;

            if (transform.root.GetComponent<ClientTankManager>().activeTank != null)
            {
                if (activeTankDurability == null || motor == null)
                {
                    activeTankDurability = transform.root.GetComponent<ClientTankManager>()
                        .activeTank.GetComponent<PlayerDurability>();
                    motor = transform.root.GetComponent<ClientTankManager>()
                        .activeTank.GetComponent<TankMotor>();
                }
                maxIntegrity = activeTankDurability.defaultIntegrity;
                integrity = activeTankDurability._Integrity;



                // Calculate the color interpolation based on health
                front = activeTankDurability.ERA_Front / activeTankDurability.defaultERA;
                left = activeTankDurability.ERA_Left / activeTankDurability.defaultERA;
                right = activeTankDurability.ERA_Right / activeTankDurability.defaultERA;
                rear = activeTankDurability.ERA_Rear / (activeTankDurability.defaultERA - activeTankDurability.defaultERA / 2);
                main = integrity / maxIntegrity;
                engine = motor.engine / 1f;

            }

            // Interpolate between minColor and maxColor based on health
            //Debug.Log(lerpedColor); 

            // Update the image's color, could probably do it with array but my damns
            ColorLerp(Front, front);
            ColorLerp(Left, left);
            ColorLerp(Right, right);
            ColorLerp(Rear, rear);
            ColorLerp(Main, main);
            ColorLerp(EngineIndicator, engine);
            if(TrophyIndicator != null)
            TrophyIndicator.color = activeTankDurability.trophyEnabled ? Color.green : Color.red;
        }

        private void ColorLerp(Image image, float t)
        {
            if (t > 0.5f)
                image.color = Color.Lerp(yellow, green, Mathf.InverseLerp(0.5f, 1f, t));
            else
                image.color = Color.Lerp(red, yellow, Mathf.InverseLerp(0f, 0.5f, t));
        }
    }


}