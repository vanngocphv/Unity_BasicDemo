# Program: Basic Demo Unity
Program description: This program just using for demo/learn demo, for only purpose learning and self teaching myself. Main feature: Basic Movement, Basic Rotate Camera, Basic Inverse Kinematic, Basic Interact with interactable items <br />
Creator: NgocPHV <br />
Date Created: 27/06/2023 - 17:30 (AM GMT +7) <br />
Date Updated: 29/06/2023 - 14:21 (PM GMT +7)  <br />
Date Finished: - <br />

# Index content
* [General info](#general-info)
* [Technologies](#technologies)
* [Update info](#update-info)
* [Feature](#feature)
* [Explain](#explain)
* [Demo](#demo)

# General info
- (*Update soon)

# Technologies
- (*Update soon)

# Update info
## 29/06/2023 - 14:21 (GMT +7):
- Add Inverse Kinematic for Foot by using Animator IK
- This is still has alot of bug in Foot when go to "Walkable" tag

# Feature
- (*Update soon)

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

