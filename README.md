# CREST-RASM Digital Twin

## Controls
### Keyboard
Movement - WASD

Look - MouseXY

System Menu - Escape

Jump - SpaceBar
    Can jump twice

Interact - E
    Typically picks up items

Alternate Interact - F
    Typically pushes buttons, places items, etc

Equip Item - Y
    With no item equipped

Unequip Item - Y
    With item equipped

Throw Item - LeftClick
    Item velocity increases the longer it is held up to 3 seconds

Reset Position - X
    Spawns above platform

Toggle Bag - B
    Enables and disables the inventory bag

UI Left Click - LeftClick
    Clicks world space UI

### VR
Movement - Left Thumbstick

Look - Headset Tracking / Right Thumbstick X
    Can rotate player with Right Thumbstick X axis

Jump - A
    Can jump twice

Toggle Bag - B
    Enables and disables the inventory bag

Reset Position - X
    Spawns above platform

System Menu - Y

Equip Item - Backpack
    Reach into red backpack behind player to equip item

Unequip Item - Backpack
    Reach into red backpack behind player and drop held item

Grab - Left Grip / Right Grip

UI Select - Left Trigger / Right Trigger

## Workflow
```New Files``` are files added as part of a feature branch
```Existing Files``` are files that have been fully integrated into the main branch and exist as a dependency for features present in all branches

### Assets
```New Files``` may be added as needed

All ```New Files``` must reside in ```Assets/Members/{YourName}```

```Existing files``` outside of ```Assets/Members/{YourName}``` are not to be modified to prevent merge conflicts and versioning issues. If a feature needs to be added to an existing file, reach out to ```Feniren``` for feature update discussions to maintain parity between branches

### Code
Code within ```New Files``` may be written with any desired style and structure

Code is subject to all conditions specified in the Pull Requests section

### Prefabs
Prefabs that are ```Existing Files``` are subject to the modification restrictions. If a modification is needed for feature development, create a prefab variant

### Pull Requests
When a feature is completed and ready to be merged into the main branch, a pull request must be made. Pull requests will be reviewed and approved as long as they meet the previously specified Asset requirements

Specific sections of code may be rewritten after being merged into main for readability, optimization, or uniformity

Features added to main from the pull request will remain in ```Assets/Members/{YourName}``` until complete or required for a different feature that is still a branch. When either of these conditions are met, the specific required features will transition from ```New Files``` to ```Existing Files```and be moved from ```Assets/Members/{YourName}``` to the appropreate directory and are subject to the ```Existing Files``` restrictions

## AI Usage
AI usage is fully permitted as long as the end result abides the Workflow restrictions

Any source code submitted to LLMs is utilized by the respective company for training data, and it is up to Dr. Jamshidi to determine what is an acceptable export of source code