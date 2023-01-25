This is an inventory/equipment system currently in use for a personnal Action RPG / Roguelite Multiplayer Dungeon Crawler game.

Inventory and equipment systems are database persistent, so all datas are commited to the game backend throught a REST Api.
They are basically a normal RPG inventory and equipment system so the player can equip stuffs and weapons, place them in the inventory, get loots from killing enemies or openning chest, etc...

Inventory and Equipment systems are made in two parts, the system itself which handle the datas and do the actual operation on them, 
and the controllers which implement the UI logic and the player interactions.

Systems and controllers are communicating via c# events (this observer pattern is mainly used accros all my project).


