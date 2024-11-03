using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using Cinemachine;
//lighting 
using UnityEngine.Rendering.Universal;
using FishNet.Connection;
using FishNet.Object.Synchronizing;

namespace TankRoulette
{
    public class CameraController : NetworkBehaviour
    {

        private bool initialized = false;
        public BootstrapNetworkManager BNM;
        [SerializeField] private GameObject GameManager;

        [SerializeField] private Camera ReferenceCamera;

        [SerializeField]
        private CinemachineVirtualCamera mainCamera;


        public Camera SecondaryCamera;

        public Transform cameraCenter;
        public GameObject tank;


        [SerializeField]
        private Transform scoutVisionCenter;

        private Light2D tankTurretVision;

        [SerializeField]
        private Light2D scoutVision;

        [SerializeField]
        private EdgeCollider2D edge;
        private Vector2[] edgePoints;

        [SerializeField]
        BoxCollider2D Top;
        [SerializeField]
        BoxCollider2D Left;
        [SerializeField]
        BoxCollider2D Bottom;
        [SerializeField]
        BoxCollider2D Right;

        public bool reachedEdgeX = false;
        public bool reachedEdgeY = false;


        public bool scoutMode = false;

        public bool updateVisionTaper = false;

        [SyncVar]
        public float visionTaperAmount = 0.15f;


        private int edgeScrollSize = Screen.width % 100 + 90;
        private float scrollSpeed;
        public float newZoom;

        [SerializeField]
        private Vector2 moveDir;

        private Vector2 tempTankPos;
        [SerializeField]
        private float scroll = 0f;



        //private float srollPrev = 0f;

        public override void OnStartClient()
        {
            base.OnStartClient();


            if (!base.IsOwner)
            {
                this.enabled = false;
            }


            Physics2D.IgnoreLayerCollision(0, 6);
            //StandaloneAddCollider();


            /*
            GameObject LevelBounds = GameObject.Find("LevelBounds");
            Top = LevelBounds.transform.GetChild(0).GetComponent<BoxCollider2D>();
            Left = LevelBounds.transform.GetChild(1).GetComponent<BoxCollider2D>();
            Bottom = LevelBounds.transform.GetChild(2).GetComponent<BoxCollider2D>();
            Right = LevelBounds.transform.GetChild(3).GetComponent<BoxCollider2D>();
            */
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            if (base.IsServerOnly)
            {
                this.enabled = false;
                edge.enabled = false;

            }
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
        }

        public bool InitializeComponents()
        {
            try
            {
                GameManager = GameObject.Find("GameManager");
                ReferenceCamera = GameManager.GetComponent<Camera>();
                mainCamera = GameObject.Find("PrimaryCamera").GetComponent<CinemachineVirtualCamera>();
                SecondaryCamera = GameObject.Find("SecondaryCamera").GetComponent<Camera>();
                SecondaryCamera.enabled = false;
                cameraCenter = tank.transform.GetChild(0).transform;
                tankTurretVision = tank.transform.GetChild(0).GetChild(0).GetComponent<Light2D>();
                scoutVision = tank.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Light2D>();
                scoutVision.GetComponent<Light2D>().intensity = 0;
                //extremely productive use of resources here
                scoutVision.enabled = false;
                scoutVision.enabled = true;
                scoutVision.enabled = false;
                Debug.Log("Camera Controller initialization - OK");
                BNM.loadingProgress.targetFill += 0.34f;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("camera controller had missing components, retrying...");
                Debug.LogWarning(ex);
                return false;
            }
            return true;


        }

        private void Awake()
        {

            // ReferenceCamera = GameObject.Find("GameManager").GetComponent<Camera>();

            //mainCamera = GameObject.Find("MainCamera").GetComponent<CinemachineVirtualCamera>();
            //SecondaryCamera = GameObject.Find("SecondaryCamera").GetComponent<Camera>();
            //SecondaryCamera.enabled = false;

            Physics2D.IgnoreLayerCollision(0, 6);
            //StandaloneAddCollider();
            /*
            GameObject LevelBounds = GameObject.Find("LevelBounds");
            Top = LevelBounds.transform.GetChild(0).GetComponent<BoxCollider2D>();
            Left = LevelBounds.transform.GetChild(1).GetComponent<BoxCollider2D>();
            Bottom = LevelBounds.transform.GetChild(2).GetComponent<BoxCollider2D>();
            Right = LevelBounds.transform.GetChild(3).GetComponent<BoxCollider2D>();
            */
        }

        [ServerRpc(RunLocally = true)]
        public void UpdateBinos(CameraController cam, float amount)
        {
            cam.visionTaperAmount = amount;
        }



        // Update is called once per frame
        void Update()
        {
            if (!initialized)
            {
                initialized = InitializeComponents();
                return;
            }



            scroll -= Input.mouseScrollDelta.y;
            scroll = Mathf.Clamp(scroll, 10f, 75f);
            mainCamera.m_Lens.OrthographicSize = scroll;



            //when the scout vision is deactivated
            if (Input.GetKeyUp(KeyCode.Mouse1))
            {
                SecondaryCamera.enabled = false;
                newZoom = 50f;
                //moveDir = cameraCenter.position;
                scoutVision.intensity = 0;
                tankTurretVision.intensity = 1;
                scoutMode = false;



                //scout vision script is enabled and disabled instantly.
                //for some reason this restarts the shadow renderer so the shadows are shown properly
                //this prevents seeing through walls
                scoutVision.enabled = false;
                //LinEarly inteRPolate the zoom, that is, zoom smoothly over 2-3 seconds
                //LerpCameraZoom(mainCamera.m_Lens.OrthographicSize, newZoom);
                //scroll = 50f;
                scoutVision.enabled = true;

            }

            //when the scout vision is activated
            else if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                SecondaryCamera.enabled = true;
                scoutVision.intensity = 1;
                tankTurretVision.intensity = 0;
                transform.position = cameraCenter.position;
                scoutVision.transform.position = tankTurretVision.transform.position;
                //tempTankPos = tank.transform.position;
                scoutMode = true;
                scoutVision.enabled = true;

                StopAllCoroutines();
                //LerpCameraZoom(mainCamera.m_Lens.OrthographicSize, newZoom);

            }







            if (scoutMode)
            {
                // Cursor.lockState = CursorLockMode.Confined;

                mainCamera.Follow = scoutVisionCenter;
                Vector2 inputDir = new(0, 0);
                scrollSpeed = Mathf.Sqrt(mainCamera.m_Lens.OrthographicSize) * 0.75f;
                int coordX = (int)Input.mousePosition.x;
                int coordY = (int)Input.mousePosition.y;

                #region EdgeScrolling
                coordX = Mathf.Clamp(coordX, 0, Screen.width);
                coordY = Mathf.Clamp(coordY, 0, Screen.height);

                edgeScrollSize = Screen.width % 100 + (int)scroll;

                //Debug.LogWarning("coords: (" + coordX + ", " + coordY + ")");
                //multiply screenwidth - edgescrollsize by a constant to get a curve
                if (coordX > Screen.width - edgeScrollSize)
                {
                    inputDir.x = Mathf.Clamp((coordX - (Screen.width - edgeScrollSize)) * .5f, 5f, 150f);
                    // Debug.LogWarning(inputDir.x);
                }
                if (coordX < edgeScrollSize)
                {
                    inputDir.x = Mathf.Clamp((coordX - edgeScrollSize) * .5f, -150f, -5f);
                }
                if (coordY > Screen.height - edgeScrollSize)
                {
                    inputDir.y = Mathf.Clamp((coordY - (Screen.height - edgeScrollSize)) * .5f, 5f, 150f);


                }
                if (coordY < edgeScrollSize)
                {
                    //Debug.LogWarning(Input.mousePosition.y);
                    inputDir.y = Mathf.Clamp((coordY - edgeScrollSize) * .5f, -150f, -5f);

                }
                #endregion



                if (Input.GetKey(KeyCode.Mouse1))
                {
                    moveDir = transform.right * inputDir.x + transform.up * inputDir.y;


                    transform.position += scrollSpeed * Time.deltaTime * (Vector3)moveDir;

                    scoutVisionCenter.transform.position = ((Vector2)transform.position + (Vector2)tank.transform.position) / 2;
                }

                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    transform.position = tank.transform.position;
                    scoutVisionCenter.transform.position = tank.transform.position;
                    mainCamera.Follow = tank.transform;
                    mainCamera.m_Lens.OrthographicSize = 30f;

                }

                float distanceToEdge = Vector2.Distance(tank.transform.position, transform.position);

                scoutVision.pointLightOuterRadius = 500f;

                if (distanceToEdge >= 8f)
                {
                    //calculate the taper amount which is a function of the default cqb vision divided by the distance times a constant.
                    scoutVision.pointLightOuterAngle = 120f / (distanceToEdge * visionTaperAmount);

                    //to maintain smooth transition, the constant must set the stuff inside the
                    //parenthesis to 1 at the distance from the if statement. ex: 10 (distance) / 0.1 = 1f.
                    scoutVision.pointLightOuterAngle = Mathf.Clamp(scoutVision.pointLightOuterAngle, 0f, 120f);
                    scoutVision.pointLightInnerAngle = scoutVision.pointLightOuterAngle - 2f;
                    //rate at which the camera zooms changes        //.33f is enough to keep tank in sight
                    //mainCamera.m_Lens.OrthographicSize = 30f + distanceToEdge * -0.01f;
                }
                else
                {
                    scoutVision.pointLightOuterAngle = 120f;

                }
                scoutVision.pointLightOuterAngle = Mathf.Clamp(scoutVision.pointLightOuterAngle, .1f, 120f);
                //scoutVision.pointLightInnerAngle = Mathf.Clamp(scoutVision.pointLightInnerAngle, 5f, 120f);
                scoutVision.pointLightInnerAngle = scoutVision.pointLightOuterAngle - 2f;

            }

            else
            {
                mainCamera.Follow = cameraCenter;
            }
            Vector3 temp = cameraCenter.position;
            //temp.z = -20;
            SecondaryCamera.orthographicSize = 4f;
            SecondaryCamera.transform.position = temp;

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                ZoomOut();
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                ZoomIn();
            }

        }




        private void LerpCameraZoom(float oldZoom, float newZoom)
        {
            StartCoroutine(CameraZoom(oldZoom, newZoom));
        }

        private void LerpScoutVision(float minRadius, float maxRadius)
        {
            StartCoroutine(ScoutVision(minRadius, maxRadius));
        }

        IEnumerator ScoutVision(float minRadius, float maxRadius)
        {
            float elapsed = 0;
            while (elapsed <= 2f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 4f);

                scoutVision.pointLightOuterRadius = Mathf.Lerp(minRadius, maxRadius, t);
                yield return null;
            }

        }

        IEnumerator CameraZoom(float oldZoom, float newZoom)
        {
            //float 3f is the time it takes to change zoom

            float elapsed = 0;
            while (elapsed <= 2f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 1f);

                mainCamera.m_Lens.OrthographicSize = Mathf.Lerp(oldZoom, newZoom, t);
                yield return null;
            }



        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            Debug.Log(collision.tag);
            if (collision.CompareTag("LevelBoundX"))
            {
                Debug.LogWarning("AT HUGE");
                Debug.Log("horizontal collision");
                reachedEdgeX = true;
            }
            if (collision.CompareTag("LevelBoundY"))
            {
                Debug.Log("vertical collision");

                reachedEdgeY = true;
            }

        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("LevelBoundX"))
            {
                reachedEdgeX = false;
            }
            if (collision.CompareTag("LevelBoundY"))
            {
                reachedEdgeY = false;
            }
        }


        private void LateUpdate()
        {



        }



        private void ZoomOut()
        {
            if (Camera.main.orthographicSize >= 35)
            {
                Debug.Log("minimum zoom reached");
                return;
            }


            ReferenceCamera.orthographicSize += 4;
            StandaloneAddCollider();

            if (edge.IsTouching(Top) || edge.IsTouching(Left) || edge.IsTouching(Bottom) || edge.IsTouching(Right))
            {
                ReferenceCamera.orthographicSize -= 4;
                Debug.LogError("you have tried to cheat!");
                return;
            }

            mainCamera.m_Lens.OrthographicSize += 4;


        }

        private void ZoomIn()
        {
            if (Camera.main.orthographicSize <= 15)
            {
                Debug.Log("maximum zoom reached");
                return;
            }
            mainCamera.m_Lens.OrthographicSize -= 4;
            ReferenceCamera.orthographicSize -= 4;
            StandaloneAddCollider();

        }

        private void StandaloneAddCollider()
        {
            //if (Camera.main == null) { Debug.LogError("Camera.main not found, failed to create edge colliders"); return; }

            var cam = ReferenceCamera;
            if (!cam.orthographic) { Debug.LogError("Camera.main is not Orthographic, failed to create edge colliders"); return; }

            Vector2 bottomLeft = cam.ScreenToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
            Vector2 topRight = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, cam.pixelHeight, cam.nearClipPlane));
            Vector2 topLeft = new Vector2(bottomLeft.x, topRight.y);
            Vector2 bottomRight = new Vector2(topRight.x, bottomLeft.y);



            // add or use existing EdgeCollider2D
            var edge = GetComponent<EdgeCollider2D>() == null ? gameObject.AddComponent<EdgeCollider2D>() : GetComponent<EdgeCollider2D>();

            var edgePoints = new[] { bottomLeft, topLeft, topRight, bottomRight, bottomLeft };
            edge.points = edgePoints;
        }
    }
}