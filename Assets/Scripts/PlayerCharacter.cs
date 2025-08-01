using UnityEngine;
using KinematicCharacterController;



public struct CharacterInput {

    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
    public bool JumpSustain;
    public CrouchInput Crouch;
    public bool Flashlight;
    

}

public struct CharacterState {

    public bool Grounded;
    public Stance Stance;
    public Vector3 Velocity;
}
public enum CrouchInput { 

    None, Toggle

}

public enum Stance { 

    Stand, Crouch, Slide, Wallrun, RailGrind

}
public class PlayerCharacter : MonoBehaviour, ICharacterController
{


    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform root;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Flashlight flashlight;
    [Space]
    [SerializeField] private float walkSpeed = 20f;
    [SerializeField] private float crouchSpeed = 7f;
    [Space]
    [SerializeField] private float walkResponse = 25f;
    [SerializeField] private float crouchResponse = 20f;

    [SerializeField] private float airSpeed = 15f;
    [SerializeField] private float airAcceleration = 70f;
    [SerializeField] private float jumpSpeed = 20f;
    [SerializeField] private float coyoteTime = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float jumpSustainGravity = 0.4f;
    [SerializeField] private float gravity = -90f;
    [Space]
    [SerializeField] private float slideStartSpeed = 25f;
    [SerializeField] private float slideEndSpeed = 15f;
    [SerializeField] private float slideFriction = 0.8f;
    [SerializeField] private float slideSteerAcceleration = 5f;
    [SerializeField] private float slideGravity = -90f;

    [Space]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchHeightResponse = 15f;
    [Range(0f, 1f)]
    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchCameraTargetHeight = 0.7f;

    private CharacterState _state;
    private CharacterState _lastState;
    private CharacterState _tempState;

    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump;
    private bool _requestedCrouch;
    private bool _requestedCrouchInAir;
    private bool _requestedJumpSustain;
    private bool _requestedFlashlight;
    
    //coyote time
    private float _timeSinceUngrounded;
    private float _timeSinceJumpRequest;
    private bool _ungroundedDueToJump;

    private Collider[] _uncrouchOverlapResults;

    public void Initialize()
    {
        _state.Stance = Stance.Stand;
        _lastState = _state;
        _uncrouchOverlapResults = new Collider[8];

        motor.CharacterController = this;
    }

    public void UpdateInput(CharacterInput input) { 
    
        _requestedRotation = input.Rotation;
        
        _requestedMovement = new Vector3(input.Move.x, 0f, input.Move.y); // Take the 2d input and turn into  3d movement vector on XZ plane
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f); // clamp para impedir que diagonal va mais rapido
        _requestedMovement = input.Rotation * _requestedMovement; //Orient the input so its relative  to facing direction

        var wasRequestingJump = _requestedJump;
        _requestedJump = _requestedJump || input.Jump;
        if (_requestedJump && !wasRequestingJump)
            _timeSinceJumpRequest = 0f;

        _requestedJump = _requestedJump || input.Jump;
        _requestedJumpSustain = input.JumpSustain;
        

        var wasRequestingCrouch = _requestedCrouch;
        _requestedCrouch = input.Crouch switch { 
        
            CrouchInput.Toggle => !_requestedCrouch,
            CrouchInput.None => _requestedCrouch,
            _ => _requestedCrouch
        };
        if (_requestedCrouch && !wasRequestingCrouch)
            _requestedCrouchInAir = !_state.Grounded;
        else if (!_requestedCrouch && wasRequestingCrouch)
            _requestedCrouchInAir = false;

        _requestedFlashlight = input.Flashlight;


    }



    public void UpdateBody( float deltaTime) { 
    
        var currentHeight = motor.Capsule.height;
        var normalizedHeight = currentHeight / standHeight;
        
        var cameraTargetHeight = currentHeight * (
            _state.Stance is Stance.Stand
                ? standCameraTargetHeight
                : crouchCameraTargetHeight
        );
        var rootTargetScale = new Vector3(1f, normalizedHeight, 1f);


        cameraTarget.localPosition = Vector3.Lerp(
            a: cameraTarget.localPosition,
            b: new Vector3( 0f, cameraTargetHeight, 0f),
            t: 1f- Mathf.Exp( -crouchHeightResponse * deltaTime)
            );

        root.localScale = Vector3.Lerp(
            a: root.localScale,
            b: rootTargetScale,
            t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
            );

    }
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
        
        // if on ground
        if (motor.GroundingStatus.IsStableOnGround) {

            _timeSinceUngrounded = 0f;
            _ungroundedDueToJump = false;

            // snap requested movement direction to the angle of the surface currently on                
            var groundedMovement = motor.GetDirectionTangentToSurface(
                direction: _requestedMovement,
                surfaceNormal: motor.GroundingStatus.GroundNormal
                ) * _requestedMovement.magnitude;


            // START SLIDING
            {
                var moving = groundedMovement.sqrMagnitude > 0f;
                var crouching = _state.Stance is Stance.Crouch;
                var wasStanding = _lastState.Stance is Stance.Stand;
                var wasInAir = !_lastState.Grounded;

                if ( moving && crouching && (wasStanding || wasInAir)) {

                    _state.Stance = Stance.Slide;


                    if (wasInAir) {

                        currentVelocity = Vector3.ProjectOnPlane(

                            vector: _lastState.Velocity,
                            planeNormal: motor.GroundingStatus.GroundNormal
                        );
                    }

                    var effectiveSlideSpeed = slideStartSpeed;
                    if (!_lastState.Grounded && !_requestedCrouchInAir) { 
                    
                        effectiveSlideSpeed = 0f;
                        _requestedCrouchInAir = false;
                    
                    }



                    var slideSpeed = Mathf.Max(slideStartSpeed, currentVelocity.magnitude);
                    
                    currentVelocity = motor.GetDirectionTangentToSurface(
                        direction: currentVelocity,
                        surfaceNormal: motor.GroundingStatus.GroundNormal
                    ) * slideSpeed;

                }


            }

            //MOVE
            if (_state.Stance is Stance.Stand or Stance.Crouch) {
                
                var speed = _state.Stance is Stance.Stand
                    ? walkSpeed
                    : crouchSpeed;

                var response = _state.Stance is Stance.Stand
                    ? walkResponse
                    : crouchResponse;

                // move along the froun on that direction
                var targetVelocity = groundedMovement * speed;

                currentVelocity = Vector3.Lerp(
                    a: currentVelocity,
                    b: targetVelocity,
                    t: 1f - Mathf.Exp(-response * deltaTime)
                    );
            
            } 
            // Continue Sliding
            else {

                // Friction
                currentVelocity -= currentVelocity * (slideFriction * deltaTime);

                // Slope
                {
                    var force = Vector3.ProjectOnPlane(
                        vector: -motor.CharacterUp,
                        planeNormal: motor.GroundingStatus.GroundNormal
                        ) * slideGravity;

                    currentVelocity -= force * deltaTime;
                }


                // Steer
                {
                    var currentSpeed = currentVelocity.magnitude;
                    var targetVelocity = groundedMovement * currentSpeed;
                    var steerForce = (targetVelocity - currentVelocity) * slideSteerAcceleration * deltaTime;
                    // Add steer force, but clamp speed
                    currentVelocity += steerForce;
                    currentVelocity = Vector3.ClampMagnitude(currentVelocity, currentSpeed);

                    // Stop
                    if (currentVelocity.magnitude < slideEndSpeed) {

                        _state.Stance = Stance.Crouch;
                    } 
                }
            }

        }
        else {
            
            
            _timeSinceUngrounded += deltaTime;
            
            
            // Move on air
            if (_requestedMovement.sqrMagnitude > 0f) {

                //Requested movement projected onto movement plane
                var planarMovement = Vector3.ProjectOnPlane(
                        vector: _requestedMovement,
                        planeNormal: motor.CharacterUp
                    ) * _requestedMovement.magnitude;

                //Current velocity on movement plane
                var currentPlanarVelocity = Vector3.ProjectOnPlane(
                    vector: currentVelocity,
                    planeNormal: motor.CharacterUp
                    );

                //Calculate movement force
                var movementForce = planarMovement * airAcceleration * deltaTime;


                // if moving slower than the max air speed, treat movementFOrce as a simple steering force
                if (currentPlanarVelocity.magnitude < airSpeed)
                {

                    // Add to the current planar velocity for a target velocity
                    var targetPlanarVelocity = currentPlanarVelocity + movementForce;

                    // Limit targe velocity to air speed
                    targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);

                    // Steer towards current velocity
                    currentVelocity += targetPlanarVelocity - currentPlanarVelocity;

                }
                // Otehrwise, nerf the movement force when it is in the direction of the current planar velocity
                // to prevent acceleration further beuond the max air speed
                else if (Vector3.Dot(currentPlanarVelocity, movementForce) > 0f) {

                    var constrainedMovementForce = Vector3.ProjectOnPlane(
                        vector: movementForce,
                        planeNormal: currentPlanarVelocity.normalized
                    );

                    movementForce = constrainedMovementForce;
                }

                // Prevent air-climbing steep slopes
                if (motor.GroundingStatus.FoundAnyGround) {

                    // if moving in the same direciotn as the resultant velocity
                    if (Vector3.Dot(movementForce, currentVelocity + movementForce) > 0f) { 

                        // Calculate obstruction normal
                        var obstructionNormal = Vector3.Cross
                        (
                            motor.CharacterUp,
                            Vector3.Cross(
                                motor.CharacterUp,
                                motor.GroundingStatus.GroundNormal
                                )
                        ).normalized;

                        // Project movemnt force onto obstrucition Plane
                        movementForce = Vector3.ProjectOnPlane(movementForce, obstructionNormal);
                    }

                    

                }

                currentVelocity += movementForce;
            }

            //Gravity
            var effectiveGravity = gravity;
            var verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            if (_requestedJumpSustain && verticalSpeed > 0f)
                effectiveGravity *= jumpSustainGravity;
           
            currentVelocity += motor.CharacterUp * effectiveGravity * deltaTime;
        }

        if (_requestedJump) {

            var grounded = motor.GroundingStatus.IsStableOnGround;
            var canCoyoteJump = _timeSinceUngrounded < coyoteTime;

            if (grounded || canCoyoteJump && !_ungroundedDueToJump) {

                _requestedJump = false; //Unset jump request
                _requestedCrouch = false; //Unset crouch request
                _requestedCrouchInAir = false;

                // Unstick to the ground
                motor.ForceUnground(time: 0f);
                _ungroundedDueToJump = true;


                //Set minimum vertical speed to the jump speed
                var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);

                //Add the difference in current and target vertical speed to character velocity
                currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);

            }
            else {

                _timeSinceJumpRequest += deltaTime;

                // if coyote time has passed...
                var canJumpLater = _timeSinceJumpRequest < coyoteTime;
                // Deny jump request
                _requestedJump = false;
            }


        }

        if (_requestedFlashlight) {

            _requestedFlashlight = false;
            flashlight.Toggle();
        
        
        
        }
    

    
    }
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {

        // Update the characters rotation to face the same direction the requested rotation (camera rotation)

        // Dont wnt to pitch up and down, so "flat" the directon the player looks. Done by projection camera direction onto flat plane.
        var forward = Vector3.ProjectOnPlane(
            _requestedRotation * Vector3.forward,
            motor.CharacterUp
        );

          if (forward != Vector3.zero)
          currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
    }
    public void BeforeCharacterUpdate(float deltaTime)   {

        _tempState = _state;
        // Crouch
        if (_requestedCrouch && _state.Stance is Stance.Stand) {

            _state.Stance = Stance.Crouch;
            motor.SetCapsuleDimensions(
                radius: motor.Capsule.radius,
                height: crouchHeight,
                yOffset: crouchHeight * 0.5f
                );
        
        }
    
    
    
    }

    public void PostGroundingUpdate(float deltaTime)   { 
    
        if (!motor.GroundingStatus.IsStableOnGround && _state.Stance is Stance.Slide) {

            _state.Stance = Stance.Crouch;
        }
    
    }

    public void AfterCharacterUpdate(float deltaTime) {

        //Uncrouch
        if (!_requestedCrouch && _state.Stance is not Stance.Stand) {

            // try to stand
            motor.SetCapsuleDimensions(
                radius: motor.Capsule.radius,
                height: standHeight,
                yOffset: standHeight * 0.5f
            );

            // check if capsule ovelaps any collider before allow to stand
            var pos = motor.TransientPosition;
            var rot = motor.TransientRotation;
            var mask = motor.CollidableLayers;
            if (motor.CharacterOverlap(pos, rot, _uncrouchOverlapResults, mask, QueryTriggerInteraction.Ignore) > 0) {
            
                // Re-crouch
                _requestedCrouch = true;
                motor.SetCapsuleDimensions(
                    radius: motor.Capsule.radius,
                    height: crouchHeight,
                    yOffset: crouchHeight * 0.5f
                );
            }
            else {

                _state.Stance = Stance.Stand;
            
            }
        }

        // update state to reflet relavent motor properties
        _state.Grounded = motor.GroundingStatus.IsStableOnGround;
        _state.Velocity = motor.Velocity;
        //Update _lasttate to store character state snapshot taken  ar begining of character update
        _lastState = _tempState;
    
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }


    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

    public bool IsColliderValidForCollisions(Collider coll) => true;

    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }

    

    public Transform GetCameraTarget() => cameraTarget;

    public void SetPosition(Vector3 position, bool killVelocity = true) {
        
        motor.SetPosition(position);
        
        if (killVelocity ) {
            motor.BaseVelocity = Vector3.zero;
        }
    }
}
