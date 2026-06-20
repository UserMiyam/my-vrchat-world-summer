using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)] 
public class Swim_Generator_V3_Synced : UdonSharpBehaviour
{
    [Header("金魚の種類設定")]
    public int fishType = 0;

    [Header("水草モード設定")]
    public bool isWaterPlant = false;
    [SerializeField] private float swaySpeed = 1.0f;
    [SerializeField] private float floatAmplitude = 0.05f;
    [SerializeField] private float rotateAmount = 5.0f;

    [Header("移動設定")]
    [SerializeField] private float moveRadius = 3.0f;
    [SerializeField] private float boundaryTolerance = 0.5f;
    [SerializeField] private float returnToCenterThreshold = 1.0f;
    [SerializeField] private float emergencyBrakeMultiplier = 3.0f;
    [SerializeField] private float maxSpeed = 0.5f;
    [SerializeField] private float acceleration = 0.2f;
    [SerializeField] private float brakingDistance = 1.0f;
    [SerializeField] private float rotationSpeed = 2.0f;

    [Header("旋回性能の調整")]
    [Range(0f, 1f)]
    [SerializeField] private float turnSlowdownWeight = 0.8f;

    [Header("慣性（ドリフト）設定")]
    [SerializeField] private float driftDeceleration = 0.3f;

    [Header("待機時間設定")]
    [SerializeField] private float minWaitTime = 2.0f;
    [SerializeField] private float maxWaitTime = 5.0f;

    [Header("アニメーション速度設定")]
    [SerializeField] private float baseAnimSpeed = 1.0f;
    [Range(0f, 100f)]
    [SerializeField] private float waitAnimSpeedPercent = 20f;

    [Header("モデルの向き補正")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    [Header("金魚すくい追加設定")]
    [SerializeField] private float fleeSpeedMultiplier = 2.0f;
    [SerializeField] private float fleeDuration = 2.0f;
    [SerializeField] private GameObject fishModel;
    
    [UdonSynced] private bool isCaught = false;

    private Animator animator;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private float waitTimer = 0f;
    private float currentSpeed = 0f;
    
    private Vector3 lastPosition;
    private float currentMaxSpeedMultiplier = 1.0f; 
    private bool isEmergencyReturning = false;

    private bool isFleeing = false;
    private float currentFleeTimer = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        startPosition = transform.position;
        if (fishModel == null) fishModel = gameObject;
        
        if (!isWaterPlant)
        {
            SetNextTarget();
        }
        UpdateVisualState();
    }

    void Update()
    {
        if (isCaught) return;

        if (isWaterPlant)
        {
            UpdateWaterPlantMotion();
        }
        else
        {
            if (isFleeing)
            {
                currentFleeTimer -= Time.deltaTime;
                if (currentFleeTimer <= 0f)
                {
                    isFleeing = false;
                    SetNextTarget();
                }
            }

            if (isMoving) MoveToTarget();
            else WaitAtTarget();

            CheckAndEnforceBoundary();
        }

        lastPosition = transform.position;
    }

    private void UpdateWaterPlantMotion()
    {
        float scale = Mathf.Max(transform.lossyScale.x, 0.001f);
        float time = Time.time * swaySpeed;
        
        float scaledAmplitude = floatAmplitude * scale;
        float newY = startPosition.y + Mathf.Sin(time) * scaledAmplitude;
        
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        float scaledRotate = rotateAmount * Mathf.Sqrt(scale);
        float swayRotation = Mathf.Sin(time * 0.8f) * scaledRotate;
        transform.rotation = Quaternion.Euler(rotationOffset.x, rotationOffset.y + swayRotation, rotationOffset.z + swayRotation * 0.5f);

        if (animator != null) animator.speed = baseAnimSpeed;
    }

    public override void OnDeserialization()
    {
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (fishModel != null) fishModel.SetActive(!isCaught);
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = !isCaught;
    }

    public void SyncCatchFishRPC()
    {
        gameObject.SetActive(true);
        isCaught = true;
        UpdateVisualState();
    }

    public void SyncRespawnFishRPC()
    {
        gameObject.SetActive(true);
        isCaught = false;
        
        if (isWaterPlant)
        {
            transform.position = startPosition;
            transform.rotation = Quaternion.Euler(rotationOffset);

            if (fishModel != null) fishModel.SetActive(true);
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            if (animator != null) 
            {
                animator.Rebind();
                animator.Update(0f);
            }
            
            UpdateWaterPlantMotion();
            return;
        }

        transform.position = startPosition;
        transform.rotation = Quaternion.Euler(rotationOffset); 
        currentMaxSpeedMultiplier = 1.0f;
        isEmergencyReturning = false;
        isFleeing = false;
        
        SetNextTarget();
        UpdateVisualState();
    }

    public void CatchFish()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        
        gameObject.SetActive(true);
        
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SyncCatchFishRPC));
        RequestSerialization(); 
    }

    public void RespawnFish()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        gameObject.SetActive(true);
        
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SyncRespawnFishRPC));
        RequestSerialization(); 
    }

    public void FleeFrom(Vector3 poiPosition)
    {
        if (isCaught || isWaterPlant || isEmergencyReturning) return;
        
        Vector3 fleeDirection = (transform.position - poiPosition).normalized;
        fleeDirection.y = 0;

        float scaledRadiusX = moveRadius * transform.lossyScale.x;
        float scaledRadiusZ = moveRadius * transform.lossyScale.z;
        Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)) * (scaledRadiusX * 0.3f);
        Vector3 newTarget = transform.position + (fleeDirection * scaledRadiusX) + randomOffset;

        Vector3 offsetFromStart = newTarget - startPosition;
        offsetFromStart.y = 0;
        if (offsetFromStart.magnitude > Mathf.Max(scaledRadiusX, scaledRadiusZ))
        {
            newTarget = startPosition;
            isEmergencyReturning = true; 
        }

        targetPosition = newTarget;
        currentMaxSpeedMultiplier = fleeSpeedMultiplier; 
        isMoving = true;
        isFleeing = true;
        currentFleeTimer = fleeDuration;
    }

    void SetNextTarget()
    {
        currentMaxSpeedMultiplier = 1.0f; 
        isEmergencyReturning = false; 
        isFleeing = false; 
        Vector2 randomCircle = Random.insideUnitCircle;
        float scaledRadiusX = moveRadius * transform.lossyScale.x;
        float scaledRadiusZ = moveRadius * transform.lossyScale.z;
        targetPosition = startPosition + new Vector3(randomCircle.x * scaledRadiusX, 0, randomCircle.y * scaledRadiusZ);
        isMoving = true;
    }

    void MoveToTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);
        float scale = Mathf.Max(transform.lossyScale.x, 0.001f);
        
        float currentMaxSpeed = maxSpeed * scale * currentMaxSpeedMultiplier;
        float currentAcceleration = acceleration * scale;
        float currentBrakingDistance = brakingDistance * scale;

        if (isEmergencyReturning) currentAcceleration *= emergencyBrakeMultiplier;

        Quaternion offsetRot = Quaternion.Euler(rotationOffset);
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction) * offsetRot;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        Vector3 realForward = transform.rotation * Quaternion.Inverse(offsetRot) * Vector3.forward;
        float desiredSpeed = currentMaxSpeed;
        if (distance < currentBrakingDistance) desiredSpeed *= (distance / currentBrakingDistance);

        float dot = Vector3.Dot(realForward, direction); 
        desiredSpeed *= Mathf.Lerp(1.0f, Mathf.Clamp01(dot), turnSlowdownWeight);

        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, currentAcceleration * Time.deltaTime);
        transform.position += realForward * currentSpeed * Time.deltaTime;

        UpdateAnimator(currentMaxSpeed);

        float arriveThreshold = isEmergencyReturning ? (returnToCenterThreshold * scale) : (0.1f * scale);
        if (distance < arriveThreshold || (distance < 0.3f * scale && currentSpeed < 0.01f))
        {
            isMoving = false;
            isEmergencyReturning = false; 
            isFleeing = false; 
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
        }
    }

    void WaitAtTarget()
    {
        float scale = Mathf.Max(transform.lossyScale.x, 0.001f);
        float currentMaxSpeed = maxSpeed * scale;
        if (currentSpeed > 0f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, driftDeceleration * scale * Time.deltaTime);
            Vector3 realForward = transform.rotation * Quaternion.Inverse(Quaternion.Euler(rotationOffset)) * Vector3.forward;
            transform.position += realForward * currentSpeed * Time.deltaTime;
            UpdateAnimator(currentMaxSpeed);
        }

        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0) SetNextTarget();
    }

    private void CheckAndEnforceBoundary()
    {
        float scale = Mathf.Max(transform.lossyScale.x, 0.001f);
        float maxAllowedRadius = (moveRadius + boundaryTolerance) * scale;
        Vector3 offsetFromStart = transform.position - startPosition;
        offsetFromStart.y = 0;

        if (offsetFromStart.magnitude > maxAllowedRadius)
        {
            transform.position = startPosition + offsetFromStart.normalized * maxAllowedRadius;
            targetPosition = startPosition;
            isMoving = true;
            isEmergencyReturning = true; 
            isFleeing = false;
            currentMaxSpeedMultiplier = 1.0f;
            currentSpeed *= 0.05f; 
        }
    }

    private void UpdateAnimator(float currentMaxSpeed)
    {
        if (animator != null && currentMaxSpeed > 0f)
        {
            float speedRatio = Mathf.Clamp01(currentSpeed / currentMaxSpeed);
            float waitAnimSpeed = baseAnimSpeed * (waitAnimSpeedPercent / 100f);
            animator.speed = Mathf.Lerp(waitAnimSpeed, baseAnimSpeed, speedRatio);
        }
    }
}