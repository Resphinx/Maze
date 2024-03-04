# Resphinx.Maze
A simple multi-level realtime random maze generator for Unity.

You can either import it as the package or via the source code. To use the maze generator you need to do a few steps:
1. Attach the component Mazer to an object on the scene. You can have  multiple mazes in a scene, however, all of them automatically become active once the game starts. You will need to pause or deactivate them via script (use pause or inGame fields of Mazer, or alternatively, change how Init is called in Mazer class).
2. Set the properties of each Mazer component (columns, rows, level count, size and height) and importantly, the Prefab Root that is the immediate parent of <b>all</b> maze element <b>sets</b> and the Character (doesn't need to be your actual character). 
3. You need to define at least one <b>set</b> of kind for floors, columns and open and closed walls, repectively, through PrefabSettings components. Please note that the properties of the elements should match the objects hierarchy.

# Elements
1. Get the element <b>sets</b> right: the elements would be children of their sets while the sets are children of the maze root. For example, a wall set would be an empty object with an unimportant position or rotation. However, it has a child wall object whose (global) position should match the centre of its cell floor, and its (global) rotation should match the direction it is supposed to be on.
2. <b>Walls</b> - if you have a set of four walls, their names should end with numbers which in order represent X-, Z-, X+ and Z+ directions. Otherwise, you can only have a set of one wall where you can set what direction it is (via the Side field of the Prefab Settings component and checking Rotatable to make sure it doesn't only apply to that side). 
3. <b>Floors</b>: ideally you will have five floor objects for each floor set (this is to help lightmapping). Each object represents a different combination of walls around the floor. The open side(s) of the floor/cell include (X-), (X-, Z-), (X-, Z-, Z+), (all sides), and (X-, X+). The paired floors however act like wall sets and their sets should be dealt with like them.
4. <b>Columns</b>: their order do not matter.
5. <b>Items</b>: their <b>id</b> should match an id that is already defined in Mazer's <b>Maze items</b> field. 
# Navigation and others
1. The maze navigation is based on a custom class UserInputs that is used work in Fixed Updates. If you want to use Unity's input logics or your own, you need to change all instances of UserInputs' Pressed and Hold references. 
2. A method EnterCell in MazeCell class is called whenever the user enters a cell. That's where you can add event-like scripts.
3. Items (as Item types in Prefab Settings) are generated as children to the cell's floor object. They can be accessed via the field <i>items</i> of each cell. 
4. Open walls can have an access constraint, by the Opening field. It indicates what portion of the wall (min 0, and max 1) can be passed through. If you are using a rotatable set, make sure that the Mirrored field is checked, in case the opening is not in the center of the wall. This option doesn't take locked doors into account. For that, you need to change the values of the 2x2 bool array <b>allowPass</b> that can control if the opening is passable or not (only works on Open walls; see the comments above the declaration of the fields for the correspondence of their indices with real world directions).
5. When a maze is created, a Vision Map is also created for it that, by default, contains only elements that are visible from each cell or around it (depending on <i>visionMode</i> of Mazer component). While this works well for 1st person views, it may lack intuitiveness for 3rd person views. You can increase the offset of the view by the <i>visionOffset</i> of Mazer component.
# Known bugs which will be fixed:
1. Dashing in the maze (with Space key) can be buggy if the space is pressed during the dash. 
2. For limited open walls (with another Opening value than 0,1) may cause the player stick in the walls if entered from certain angles.

# Future todos
1. Importing text-based maze definitions.
2. Larger "paired" cells (Done).
3. Distorted mazes.
4. Growable vision map.