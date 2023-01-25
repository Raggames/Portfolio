A third person player controller and action camera controller with cam collision handling currently in use for a personnal Action RPG / Roguelite Multiplayer Dungeon Crawler game.

The player control is a base system. More subsystem can be added for optionnal functions like the rolling ability, which is implemented as a 'subsystem' of the Character entity.
The rolling function is decoupled from the system so it is possible to just remove the script from the player character prefab to unable the rolling ability.
There are more functions like this (blocking, aiming...) in the project.