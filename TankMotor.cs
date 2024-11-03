using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;



public class TankMotor : NetworkBehaviour
{

    //move data structure. this holds all the data relevant to the user input
    //movement data values are created from user input and are normalized to [-1, 1]

    public struct MoveData : IReplicateData
    {
        public float MovementInput;
        public float TurningInput;
        public bool ScoutMode;
        public float AccelerationCounter; // Updated to float
        public float Engine;
        public int SpeedBoostCounter; // Added speedBoostCounter to MoveData

        //refers to turret angle
        public float DesiredAngle;

        public bool SpeedBoost;

        public MoveData(float movement, float turning, float desiredAngle, bool scoutMode, float accelerationCounter, float engine, bool speedBoost, int speedBoostCounter) // Updated to float
        {
            MovementInput = movement;
            TurningInput = turning;
            DesiredAngle = desiredAngle;
            ScoutMode = scoutMode;
            AccelerationCounter = accelerationCounter; // Updated to float
            Engine = engine;
            SpeedBoost = speedBoost;
            SpeedBoostCounter = speedBoostCounter; // Assign the value
            _tick = 0;
        }

        //required by fishnet

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }


    //contains all the move data to reconcile (fix) with the server
    //position, rotation, and their speeds are all included + the turret's current angle
    public struct ReconcileData : IReconcileData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public float AngularVelocity;
        public float TurretAngle;
        public Quaternion TurretRotation;
        public float AccelerationCounter; // Updated to float
        public bool SpeedBoost;
        public int SpeedBoostCounter; // Added speedBoostCounter to ReconcileData

        public ReconcileData(Vector3 position, Quaternion rotation, Vector3 velocity, float angularVelocity, float turretAngle, Quaternion turretRotation, float accelerationCounter, bool speedBoost, int speedBoostCounter) // Updated to float
        {
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            AngularVelocity = angularVelocity;
            TurretAngle = turretAngle;
            TurretRotation = turretRotation;
            AccelerationCounter = accelerationCounter; // Updated to float
            SpeedBoost = speedBoost;
            SpeedBoostCounter = speedBoostCounter; // Assign the value
            _tick = 0;
        }

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }
    [SerializeField]
    PlayerAudioController pac;

    //can be changed in inspector
    [SerializeField]
    private float _moveSpeed;
    [SerializeField]
    private float _turnSpeed;
    [SerializeField]
    private float _reverseMultiplier;

    private float _turretTurnSpeed;

    [SerializeField]
    private float _slowTurretSpeed;

    [SerializeField]
    private float _fastTurretSpeed;

    [SerializeField]
    private bool scoutMode;

    public float engine;
    public bool repairing = false;

    [SerializeField]
    private Rigidbody2D tankRB;

    [SerializeField]
    private Transform turretCenter;

    [SerializeField]
    private float desiredAngle;

    [SerializeField]
    private float accelerationCounter; // Updated to float

    [SerializeField]
    private bool speedBoost;

    [SerializeField]
    private int speedBoostCounter = 0;

    private const float MovementThreshold = 0.05f;
    private const float HighAccelerationThreshold = 100; // Updated to float
    private const float LowAccelerationThreshold = 25; // Updated to float
    private const float MaxAccelerationCounter = 200; // Updated to float

    private void Awake()
    {
        //instead of FixedUpdate, use Fishnet's timemanager to update each frame
        InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;
        InstanceFinder.TimeManager.OnPostTick += TimeManager_OnPostTick;
    }


    private void OnDestroy()
    {
        if (InstanceFinder.TimeManager != null)
        {
            InstanceFinder.TimeManager.OnTick -= TimeManager_OnTick;
            InstanceFinder.TimeManager.OnPostTick -= TimeManager_OnPostTick;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        base.PredictionManager.OnPreReplicateReplay += PredictionManager_OnPreReplicateReplay;

    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        base.PredictionManager.OnPreReplicateReplay -= PredictionManager_OnPreReplicateReplay;


    }

    /// <summary>
    /// Called every time any predicted object is replaying. Replays only occur for owner.
    /// Currently owners may only predict one object at a time.
    /// </summary>
    private void PredictionManager_OnPreReplicateReplay(uint arg1, PhysicsScene arg2, PhysicsScene2D arg3)
    {
        /* Server does not replay so it does
         * not need to add gravity. */
        if (!base.IsServer) { }
        // AddGravity();
    }

    //fishnet's built in update function. it overrides the update but only for this script
    private void TimeManager_OnTick()
    {
        MoveData moveData;
        //runs on client side
        if (base.IsOwner)
        {
            //reconcilliate first
            Reconciliation(default, false);
            //gather user input and put it into movedata "structure"
            GatherInputs(out MoveData md);
            moveData = md;
            //move player using the data stored in movedata
            Move(md, false);

            // Refactored method call
            UpdateAccelerationCounter(md);

        }
        if (base.IsServer)
        {
            //server is master, therefore it does not need to reconcilliate or gather inputs
            Move(default, true);
        }

    }






    //object's transform will be changed after the physics simulation,
    //so data after the simulation, which is POST tick
    private void TimeManager_OnPostTick()
    {

        //EXTREMELY IMPORTANT to send anything that might affect the transform of an object!
        //this also includes any colliders attatched to the object
        //see video for full explanation

        /* Reconcile is sent during PostTick because we
             * want to send the rb data AFTER the simulation. */
        if (base.IsServer)
        {
                if(NetworkManager.TimeManager.Tick % 3 == 0)
{
            //all the stuff included in this is explained in "ReconcileData"
            ReconcileData rd = new ReconcileData(transform.position,
                transform.rotation, tankRB.velocity, tankRB.angularVelocity,
                desiredAngle, turretCenter.rotation, accelerationCounter, speedBoost, speedBoostCounter); // Added speedBoostCounter
            Reconciliation(rd, true);
}
        }

        //in general, the "runningAsServer" parameter
        //should be set to true when: SERVER has (DATA) to send to => CLIENT
        //should be set to false when: CLIENT has (DATA) to send to => SERVER

    }


    private void GatherInputs(out MoveData data)
    {
        data = default;
        
        //Input Axis settings can be changed in the Project Settings Under Input Manager
        float movement = Input.GetAxis("Vertical");


        float rotation = -1 * Input.GetAxis("Horizontal");

        bool scoutMode = Input.GetKey(KeyCode.Mouse1);

        //Get mousePosition as a Vector3
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.nearClipPlane;
        Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        Vector2 targetDirection = (Vector3)mouseWorldPosition - turretCenter.transform.position;

        //do some magic with math to get the desired angle of the turret
        desiredAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        speedBoost = Input.GetKey(KeyCode.LeftShift);
        // Debug.LogWarning(desiredAngle);

        /*
         * unfortunately, need to have the movement and 
         * rotation always send because the turret will almost always be moving
        if (movement == 0f && rotation == 0f)
        {
            return;
        }
        */
        //return the data as new MoveData struct
        data = new MoveData(movement, rotation, this.desiredAngle, scoutMode, this.accelerationCounter, this.engine, this.speedBoost, this.speedBoostCounter); // Add this.speedBoostCounter


    }

    [Replicate]
    private void Move(MoveData data, bool asServer, Channel channel = Channel.Unreliable, bool replaying = false)
    {
        if (data.ScoutMode)
        {
            _turretTurnSpeed = _slowTurretSpeed;
        }
        else
        {
            _turretTurnSpeed = _fastTurretSpeed;
        }
        
        Quaternion targetRotation =  Quaternion.Euler(0, 0, data.DesiredAngle - 90);

        //rotate the turret only using the desired angle as a goal
        turretCenter.rotation = Quaternion.RotateTowards(turretCenter.rotation, targetRotation,
            (float)(_turretTurnSpeed * TimeManager.TickDelta));

//        Debug.LogWarning("angle difference: " +Quaternion.Angle(turretCenter.rotation, targetRotation));
        pac.PlayTurret(Quaternion.Angle(turretCenter.rotation, targetRotation) > .1f);
        

        if (data.Engine <= 0)
        {
            return;
        }

        float forceVal;
        float acceleration;

        if(repairing)
        {
            accelerationCounter = 0;
            return;
        }
//        Debug.LogWarning(acceleration);
        if (data.MovementInput >= MovementThreshold || data.MovementInput <= -MovementThreshold)
        {
            ApplyTurningPenalty(data.TurningInput, data.SpeedBoost);
        }
        else
        {
            accelerationCounter = LowAccelerationThreshold;
        }

        //function to calculate acceleration which is multiplied by the force
        acceleration = Mathf.Clamp(data.AccelerationCounter * (data.SpeedBoost ? 0.02f : 0.015f), 0.75f, 1.9f);
//        Debug.LogWarning(acceleration);
        //if the tank is reversing, go apply reversemultiplier (60%) speed going backwards
        pac.PlayEngine(tankRB.velocity.magnitude,  Mathf.Abs(tankRB.angularVelocity));

        if (data.MovementInput <= -MovementThreshold)
        {
            forceVal = Mathf.Ceil(data.MovementInput * _moveSpeed * _reverseMultiplier);
        }
        else
        {
            forceVal = data.MovementInput * _moveSpeed;
        }
        

        forceVal *= acceleration;

        //engine is a durability element that reflects overall speed of tank
        //SCN had the idea to create a piecewise function based on the engine durability
        //ex: 1-0.85 = 100% effectiveness, 0.85-0.6 = 90%, 0.6-0.3 = 75% effectiveness, and 0.3 - 0.0 = 25% effectivenss

        float EngineEffectiveStrength;

        if (engine >= 0.85)
        {
            EngineEffectiveStrength = 1f;
        }
        else if (engine >= 0.6)
        {
            EngineEffectiveStrength = 0.9f;
        }
        else if (engine >= 0.3)
        {
            EngineEffectiveStrength = 0.75f;
        }
        else
        {
            EngineEffectiveStrength = 0.25f;
        }

        if (data.SpeedBoost)
        {
            speedBoostCounter++;
            if (speedBoostCounter >= 10)
            {
                engine -= 0.005f;
                speedBoostCounter = 0; // Reset the counter
            }
        }
  

        Vector3 force = EngineEffectiveStrength * forceVal * transform.up;
        force *= data.SpeedBoost ? 1.15f : 1f;
        //im literally freaking out right now, and it FUCKING DELETED THIS COMMENT i had to remake it
        //it is inferring that the left shift key is being used for the speed boost which is crazyyyy
        //it is also inferring that the speed boost should apply a 1.6x force increase to the tank, which is also insane.

        //same code you would see if this was single player only
        //add forward force to the tank
        tankRB.AddForce(force, ForceMode2D.Force);



        //change angular velocity (rotation) based on turning input and engine power
        tankRB.angularVelocity = engine * data.TurningInput * _turnSpeed * (data.SpeedBoost ? 1.35f : 1f);

    }

    [Reconcile]
    private void Reconciliation(ReconcileData data, bool asServer, Channel channel = Channel.Unreliable)
    {
        transform.SetPositionAndRotation(data.Position, data.Rotation);
        tankRB.velocity = data.Velocity;
        tankRB.angularVelocity = data.AngularVelocity;
        turretCenter.rotation = data.TurretRotation;
        desiredAngle = data.TurretAngle;
        //required for CSP
        accelerationCounter = data.AccelerationCounter;

        // Reconcile speedBoostCounter
        speedBoostCounter = data.SpeedBoostCounter;
    }

    private bool IsTankMoving(float movementInput)
    {
        return Math.Abs(movementInput) >= MovementThreshold;
    }

    private void ApplyTurningPenalty(float turningInput, bool speedBoost)
    {
        if (IsTankTurning(turningInput))
        {
            DecreaseAccelerationCounter(speedBoost);
        }
        else
        {
            IncreaseAccelerationCounter(speedBoost);
        }
    }

    private bool IsTankTurning(float turningInput)
    {
        return Math.Abs(turningInput) >= MovementThreshold;
    }

    private void DecreaseAccelerationCounter(bool speedBoost)
    {
        if (accelerationCounter <= 0) return;

        if (accelerationCounter > HighAccelerationThreshold)
        {
            accelerationCounter -= speedBoost ? .15f : .25f;
        }
        else if (accelerationCounter > LowAccelerationThreshold)
        {
            accelerationCounter--;
        }
    }

    private void IncreaseAccelerationCounter(bool speedBoost)
    {
        if (accelerationCounter < MaxAccelerationCounter)
        {
            accelerationCounter += speedBoost ? .35f : .1f;
        }
    }

    private void UpdateAccelerationCounter(MoveData data)
    {
        if (IsTankMoving(data.MovementInput))
        {
            ApplyTurningPenalty(data.TurningInput, data.SpeedBoost);
        }
        else
        {
            accelerationCounter = LowAccelerationThreshold;
        }
    }
}