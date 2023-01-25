This is a sample from the Ability System currently in use for a personnal Action RPG / Roguelite Multiplayer Dungeon Crawler game.

In th game, the player is totally free upon deck building his own character. 
There is no class restriction, but an equipment/weapon deck building oriented logic.

Each ability is a child class of 'Ability'.
An ability controller controls the interfacing between player input and actual launching of the ability.
Each ability is responsible of implementing its gameplay logic, controlling the feedbacks, visual and sound effects, etc...
The ability is a little gameplay module that can be added to the character.

Abilities are also used by the AI.

The abilities are also supporting a full talent/modifier system that allows the player to modify each ability to fit his gameplay needs.
