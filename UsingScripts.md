# Using These Scripts
a guide by pradhyot.N

* this guide is intended to assist you whilst using my specific scripts or just to give you an idea on how to use some of the ideas i have installed
* this guide will not go over what is inside the script as its only meant to inform you about how to install the script into unity
* you are able to change scripts as you please
* open the unity package file in a new unity project if you want to see the prototype area for yourself and play around in it

## Table of Contents
- [Movement Script](#movement-script--player-controller)
- [Camera Script](#camera-scripts)
- [Collectible Scripts](#collectible-scripts)
- [Enemy AI Script & AI Combat Script](#enemy-ai--ai-combat)
- [Player Combat Script]
- [Complete Player Script]
- [All Scripts](#all-scripts)
## Movement script / Player controller
* note: this script is only focusing on the movement aspects of the game, if you want combat or a full movement + combat script then it will be introduced in a later interation of this file

>[Sophisticated Movement Script](playercon.cs)\
>[basic Movement Script](playercon1.cs)

### key differences between scripts
|Sophisticated|Basic|feature|
|-|-|-|
|&check;|&cross;|explainations inside the file|
|&check;|&check;|4-directional movement|
|&check;|&check;|Coyote time|
|&check;|&check;|inbuilt animations|
|&check;|&check;|jump buffer|
|&check;|&cross;|8-directional Dashing|
|&check;|&cross;|Wall jumping|
|&check;|&check;|well optimized|
|&cross;|&check;|easy to comprehend from a beginner POV|

choose the right file for you or you can pick and choose what features you want from these files

if you simply just add these script onto your player you'll notice it won't work mainly because there are a few unassigned variables that you'd have to set within unity itself

jump to your section based on what script you would follow:
- [Basic Movement Setup](#basic-movement-setup)
- [Sophisticated Movement Setup](#sophisticated-movement-setup)

### Sophisticated Movement Setup
firstly you would naturally place the script onto your player and be met with this section and you will find if you run it most of the mechanics will not work:

![Image Not found](picturesfortutorials\SophisticatedStart.png)

you'll need to add **GameObjects** with circle colliders to make it work and attach them onto the player character\
to do this you'd right click on the heirachy window to create an empty object, it will automatically have a circle collider attached to it:
![Image not found](picturesfortutorials\gameobjectcreate.png)\
then simply make the empty game object a child of the player character\
you will have to do this 2 more times and for clarity rename them so you know what is which\
so that you can actually see the collider when testing give it a tag so that you would know if its there:\
![Image not Found](picturesfortutorials\GameObject.png)\
you will now want to adjust the placement of the objects so that it aligns with the sides of the player:\
![Image not Found](picturesfortutorials\playerGameObjects.png)
* if you are having trouble with this simply adjust it in your inspector window

then simply add your game objects onto the transform sections (corresponding to where they are situated)

Now we have to register the **Ground layer**, you would've done this in the tutorials by adding tags, but that can get messy in the newer versions\
just for easier understanding: unity has many different layers that can be used it already auto assigns the layer 1-5 but all other layers are free to use for anything else

To assign the layer that will be 'ground' simply go into the inspector and change the tilemap's layer to a new level (you might have to click add layer as to be able to add your ground onto)\
example:\
![Image not Found](picturesfortutorials\tileMapSet.png)\
now assign this layer to your ground layer variable\
your script component should now look like this:\
![Image not Found](picturesfortutorials\sophisticatedFinish.png)\
finally you have to add a material that has no friction and add that onto the chosen collider as to prevent the player simply gripping onto the wall\
when you play the script now it should work as intended

### Basic Movement Setup
As you would set this component into the player character it at first wont work because it is missing a few assigned variables:\
![Image not Found](picturesfortutorials\BasicStart.png)\
Primarily Ground Check and Ground Layer, you would've assigned these with tags following Mr Scott's tutorials but these methods are easier to use and will result in less bugs overall

Firstly you will have to make a game object serve as your ground check to do this you'd right click on the heirachy window to create an empty object, it will automatically have a circle collider attached to it:\
![Image not found](picturesfortutorials\gameobjectcreate.png)\
then upon attaching this to the player object to make it a child of the player then adjust its position to make it on the bottom as it is a ground check\
you can give the game object a tag so that you can see its position to make it easier to adjust it while developing\
![Image not Found](picturesfortutorials\GameObject.png)
* if you are having issues with setting the object to the correct position use the inspector window

then simply add the game object you've made to the ground check section

Now we have to implement the **Ground Layer**\
just for easier understanding: unity has many different layers that can be used it already auto assigns the layer 1-5 but all other layers are free to use for anything else

To assign the layer that will be 'ground' simply go into the inspector and change the tilemap's layer to a new level (you might have to click add layer as to be able to add your ground onto)\
example:\
![Image not Found](picturesfortutorials\tileMapSet.png)\
now assign this layer to your ground layer variable

your script component should now look like this:\
![Image not Found](picturesfortutorials\basicfinish.png)

the script and player should now function as intended 

### OPTIONAL: using the script's animator
the script ive made hard codes the animation so youll need to change the way your animator is set out, it heavily simplifies the process, after chcages it should look something like this:
![Image not Found](picturesfortutorials\animator.png)
TLDR: remove ur animation processes and rewire kinda like the image


## Camera Scripts
> [camera_script_1](camera.cs)\
> [Camera_script_2](Camera2.cs)\
> [Camera_script_3](Camera3.cs)\
> [Teacher's_Script](CameraFollow.cs)

each script is a bit different experiment with what you'd like

also cinemachine is viable and doesn't require code, if you want to learn how to use cinemachine use this video by unity:\
[![Video Unavailable](https://img.youtube.com/vi/mWqX8GxeCBk/0.jpg)](https://www.youtube.com/watch?v=mWqX8GxeCBk)

## Collectible Scripts
> [Moving Collectible](HealthCollectible.cs)\
> [basic collectible](Collectible.cs)

these are two different scripts that do as they are named, one makes the collectible float up and down, whilst the basic is one that you would've made with mr scott/
quick reminder that you would have to set tags for both the player and the collectible and to also make the collectible's hitbox a trigger

OPTIONAL: Within the moving collectible there is a setting to add health to the player if you want to remove that simply comment out the health line/
but if you wanted to keep that place the following code into the player script:/
```c#
    public void ChangeHealth(int amount)
    {
        if (amount < 0 || amount > 0)
        {
            if (isInvincible)
            {
                return;
            }
            isInvincible = true;
            damageCooldown = timeInvincible;
            if (amount < 0)
            {
                animator.SetTrigger("Hit");
            }
        }
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UIHandler.instance.SetHealthValue(currentHealth / (float)maxHealth);
    }
```
the UI handler section only relates if you are using a ui feature    

## Enemy AI & AI Combat
This section makes the following few assumptions:/
* the enemy will preform an attack when in range of the player / use a projectile when possible
* you want enemy pathfinding
* You will not use the in built unity animator
* The player should not phase through the enemy
if any of the following doesn't apply to your game you will need to make some tweaks to the code/
fortunately you should be able to understand the script somewhat as there are comments explaining what most features do.

> [Patrolling Meele Enemy](SimpleEnemy.cs)\
> [more will be added](null)

the script's name is very misleading this script is **NOT** simple anyways links to the different installation guides:
- [General Information](#general-information) **recommened to start on**
- [Base enemy (Patrolling Meele Enemy)](#base-enemy) 
- [Projectile Shooting enemy]
- [tracker enemy]
- [boss enemy]
### General Information
before we actually start implementaion we need to fully comprehend 2 concepts;

#### **Gizmos**
the gizmos are tools that show behaviour patterns and what not in the scene view editor they will appear as coloured objects that if used correctly will be able to show us - the editors on different enemy behaviours and patterns\
example:\
![image not found](picturesfortutorials\gizmopreview.png)\
red cylinder = enemy\
cyan circle = wall collider\
green circles = ledge check & ground check\
red circle = attack radius\
yellow circle = enemy notice range

this will be extremely important later so keep note of this

#### **Tags & Layers**
you would've already done work with tags when working with Mr Scott but they are now more important than ever, they will be one of 3 traits that make the enemy pathfinding possible\
Meanwhile you would've set layers within the player controller so you should already understand how that works as well

### Base enemy
the base enemy will be the simplest to implement into your game, saying that it does look like plane controls when you open it up for the first time\
anyways upon adding the script to the player enemy the script component should look like this:\
![Image Not Found](picturesfortutorials\SimpleAIStart.png)\
some of my changes have been made as to properly test that the script works beforehand but disregard that for the moment as the placement would change dependant on the person\
Firstly you'd want to change the ground layer to be the layer where your tilemap or level is **ensure the enemy or player is not on that level**\
once that is complete you'll have to move the gizmos so that checks and whatever is able to be completed
Important info for placement:
* the enemy using this script will always default to facing right
* the cyan circle will need to be placed just directly infront of the enemy
* the ground check circle will have to be on the direct bottom of the sprite
* The Ledge Check gizmo will have to be on the bottom right corner of the sprite (just underneath the soles of the soes if you have that)
* all other circles are also adjustable but refer back to the general information section to understand what each of them are\

the enemy sprite should now looks something like this: \
![Image Not found](picturesfortutorials\setupBasicEnemy.png)\
You've just completed the hard part now all you need to do is adjust the variables to your liking and you are complete!
- you can attempt to delve into the code if you please but i really don't recommend changing too much other than what ive annotated






## Player Combat
unfinished for now
## Finalized Player Script
unfinished for now
## All Scripts
for those who just want the scripts and no waffle;
> [Sophisticated Movement Script](playercon.cs)\
> [basic Movement Script](playercon1.cs)\
> [camera_script_1](camera.cs)\
> [Camera_script_2](Camera2.cs)\
> [Camera_script_3](Camera3.cs)\
> [Teacher's_Camera](CameraFollow.cs)\
> [Moving Collectible](HealthCollectible.cs)\
> [basic collectible](Collectible.cs)\
> [Base enemy](SimpleEnemy.cs)