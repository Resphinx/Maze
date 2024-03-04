# Maze
A simple multi-level realtime maze generator for Unity.

You can either import it as the package or via the source code. To use the maze generator you need to do a few steps:
1. Attach the component Mazer to an object on the scene. You can have  multiple mazes in a scene, however, all of them automatically become active once the game starts. You will need to pause or deactivate them via script (use pause or inGame fields of Mazer, or alternatively, change how Init is called in Mazer class).
2. Set the properties of each Mazer component (columns, rows, level count, size and height) and importantly the Prefab Root that is the immediate parent of ALL maze element SETs. 
3. You need to define at least one element of kind for floors, columns and different walls, repectively, with PrefabSettings components. Please note that the properties of the elements should match the objects hierarchy.

4. Get the element SETs right: the elements would be children of their sets while the sets are children of the maze root. For example, a wall set would be an empty object with an unimportant position or rotation. However, it has a child wall object whose (global) position should match the centre of its cell floor, and its (global) rotation should match the direction it is supposed to be on.  
5. For walls: if you have a set of four walls, their names should end with numbers which in order represent X-, Z-, X+ and Z+ directions. Otherwise, you can only have a set of one wall where you can set what direction it is (via the Side field of the Prefab Settings component and checking Rotatable to make sure it doesn't only apply to that side). 
6. For floors: ideally you will have five floor objects for each floor set (this is to help lightmapping). Each object represents a different combination of walls around the floor. The open side(s) of the floor/cell include (X-), (X-, Z-), (X-, Z-, Z+), (all sides), and (X-, X+). The paired floors however act like wall sets and their sets should be dealt with like them.

7. The maze navigation is based on a custom class UserInputs that is used work in Fixed Updates. If you want to use Unity's input logics or your own, you need to change all instances of UserInputs' Pressed and Hold references. 

Known bugs which will be fixed:
1. Dashing in the maze (with Space key) can be buggy if the space is pressed during the dash. 
2. For limited open walls (with another Opening value than 0,1) may cause the player stick in the walls if entered fron certain angles.