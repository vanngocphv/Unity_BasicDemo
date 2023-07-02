# Program: Basic Demo Unity
Program description: This program just using for demo/learn demo, for only purpose learning and self teaching myself. Main feature: Basic Movement, Basic Rotate Camera, Basic Inverse Kinematic, Basic Interact with interactable items <br />
Creator: NgocPHV <br />
Date Created: 27/06/2023 - 17:30 (AM GMT +7) <br />
Date Updated: 02/07/2023 - 16:15 (PM GMT +7)  <br />
Date Finished: - <br />

# Index content
* [General info](#general-info)
* [Technologies](#technologies)
* [Update info](#update-info)
* [Feature](#feature)
* [Explain](#explain)
* [Demo](#demo)

# General info
- Basic Demo for self-teaching myself.
- User can move, rotate camera, jump, sprint.
- Feet will dynamic set position depending on ground surface, IK for foot.

# Technologies
- This Demo using Unity 2021.3.26f1
- Unity Package: Cinemachine, Input System, Animation Rigging
- Unity Asset: Starter Assets - Third Person Character Controller, POLYGON Starter Pack - Low Poly 3D Art by Synty

# Update info
## 29/06/2023 - 14:21 (GMT +7):
- Add Inverse Kinematic for Foot by using Animator IK
- This is still has alot of bug in Foot when go to "Walkable" tag

# Feature
- Rotate Camera
- Character Movement, jump, sprint
- Interact with interactable item
- IK Animation
- IK for Feet

# Explain
## Rotation Camera:
- Set the following target for camera, we will only change the rotation of this.
- By using Camera Yaw and Camera Pitch, reset the rotation for Camera Follow Target every single frame in Late Update
- The Camera Yaw will has initial data same with current camera rotation in eulerangle.y
```
CameraYaw = Camera.main.transform.rotation.eulerAngles.y;
```
- Get current mouse position from mouse input and next, set the data into Camera Yaw and Pitch
```
Vector2 currentCameraPosition = InputManager.Instance.LookVector2;
...
CameraYaw += currentCameraPosition.x;
CameraPitch += currentCameraPosition.y;
...
//clamp the angle of yaw and pitch with min and max value
//coding in there

//set camera rotation by change the rotation of follow target
CameraFollowTarget.transform.rotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0);
```

## Movement:
- Movement of character will be depended into the camera rotate value
- Get value of input from user
- Get current speed by checking if user pressed sprint button or not
- Set value for vector 3 input direction from vector 2 input user
```
Vector3 inputDirection =  new Vector3(userInput.x, 0, userInput.y).normalized;
```
- Check if input has value <> zero vector, set rotation for it:
```
//get target rotation by getting Atan2 of x and z from input Direction and adding with euler andgle y of camera
targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg 
                                + Camera.main.transform.rotation.eulerAngles.y;
//get rotation for character by using smoothdampangle
rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref _refVelocity, smoothValue);
//rotate it frame by frame
transform.rotation = Quaternion.Euler(0, rotation, 0);
```
- Get movement vector 3 by using vector forward of character multiply with euler from targetRotation (targetRotation will be placed in Y parameter of this Euler function)
- And move Character
```
Vector3 movement = Quaternion.Euler(0, targetRotation, 0) * Vector3.forward;
characterController.Move(movement.normalized * speed * Time.deltaTime);
```

## Inverse Kinematic:
- This IK has two content:
  * IK animation for interact with interactable object, [IK Animation](#ik-animation)
  * IK for feet landing in surface, [Foot IK](#foot-ik)
 
### IK Animation:
- This is using the feature of Unity, Animation Rigging
- First, Choise "Rig Setup" in Animation Rigging, Unity will automatically create a new script "Rig Builder" and set the Rig setup has been create into <br />
![Rig Builder setup](./Image/RigBuilderSetting.PNG)
- Next, create new empty object in "Rig 1" Game Object, in this new empty object, add new Component "Two Bone IK Constraint" <br />
![Two Bone IK Setup](./Image/RightTwoBoneIKConstraint.PNG) <br />
![Auto create Hint and Target](./Image/HintAndTargetAutoCreate.PNG) <br />
- Create new animation from this rigs, we wil has the animation like this
![Animation from rigs](./Gif/AnimationGrabItem.gif)<br />
- This animation has setting the IK Position on animation Event
Script (this is animation event in animation)
```
//Set position grab item
private void GrabItemPosition(AnimationEvent animationEvent)
{
    RightHandIK.position = _weaponSelect.position;
}
//When item in hand, remove collider and set kinematic to true
private void GrabItemInHand(AnimationEvent animationEvent)
{
    _weaponSelect.GetComponent<Collider>().enabled = false;
    _weaponSelect.GetComponent<Rigidbody>().isKinematic = true;
    _weaponSelect.parent = RightHandIK;
    _weaponSelect.localPosition = Vector3.zero;
    
}
//Do something when it finish, grab finish
private void GrabItemStore(AnimationEvent animationEvent)
{
    Destroy(_weaponSelect.gameObject);
}
```

### Foot IK:


  
